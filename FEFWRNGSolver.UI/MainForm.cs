using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

public sealed partial class MainForm : Form
{
    private readonly WindowText m_Text;
    private readonly MainViewModel m_ViewModel;
    private readonly Label m_Error = new() { AutoSize = true, ForeColor = SystemColors.WindowText };
    private readonly DataGridView m_Results = CreateResultsGrid();

    private static DataGridView CreateResultsGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        VirtualMode = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
    };

    public MainForm() : this(WindowsLanguages.Load(), WindowsLanguages.Save) { }

    private MainForm(LocalizationCatalog catalog, Action<string> saveLanguage)
    {
        InitializeComponent();
        m_Text = new WindowText(catalog);
        var store = new FileUnitSettingsStore((key, path) => ShowWarning(catalog.Format(key, path)));
        var searchModel = new SeedSearchViewModel(store, catalog);
        m_ViewModel = searchModel.Settings;
        m_Text.Bind(() =>
        {
            Text = m_ViewModel.Title;
            m_Results.AccessibleName = catalog["SYS_PREDICTION_RESULTS"];
        });
        MinimumSize = new Size(660, 400);
        ClientSize = new Size(920, 720);

        var settings = new GrowthSettingsPanel(m_ViewModel, m_Text) { TabIndex = 0 };
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 8), TabIndex = 1 };
        var prediction = new TabPage { AutoScrollMinSize = new Size(0, 440) };
        var search = new TabPage();
        var mapRandom = new TabPage();
        m_Text.Bind(() => prediction.Text = catalog["SYS_PREDICTION_TAB"]);
        m_Text.Bind(() => search.Text = catalog["SYS_SEARCH_TAB"]);
        m_Text.Bind(() => mapRandom.Text = catalog["SYS_MAP_RNG_TAB"]);
        var searchPanel = new SeedSearchPanel(searchModel, settings, m_Text) { Dock = DockStyle.Fill };
        search.Controls.Add(searchPanel);
        tabs.TabPages.Add(search);
        tabs.TabPages.Add(prediction);
        tabs.TabPages.Add(mapRandom);
        var mapCalculate = BuildMapRandomPage(mapRandom);
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        content.Controls.Add(tabs);
        content.Controls.Add(settings);
        Controls.Add(content);
        MainMenuStrip = CreateMenu(catalog, saveLanguage);
        Controls.Add(MainMenuStrip);

        var layout = AddTabLayout(prediction);

        var inputs = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        var seedInput = AddInput(inputs, () => catalog["SYS_GROWTH_SEED_INPUT"], m_ViewModel.GrowthSeed, value => m_ViewModel.GrowthSeed = value);
        AddInput(inputs, () => catalog["SYS_MAX_RNG_ADVANCES"], m_ViewModel.MaxAdvances, value => m_ViewModel.MaxAdvances = value);
        layout.Controls.Add(inputs, 0, 0);
        layout.Controls.Add(CreatePredictionFilters(), 0, 1);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        var calculate = new Button { AutoSize = true };
        m_Text.Bind(() => calculate.Text = catalog["SYS_CALCULATE"]);
        calculate.Click += (_, _) =>
        {
            m_ViewModel.Calculate();
            RefreshPredictionResults();
        };
        actions.Controls.Add(calculate);
        m_Error.Anchor = AnchorStyles.Left;
        actions.Controls.Add(m_Error);
        tabs.SelectedIndexChanged += (_, _) =>
        {
            settings.Visible = tabs.SelectedTab != mapRandom;
            AcceptButton = tabs.SelectedTab == mapRandom ? mapCalculate : tabs.SelectedTab == prediction ? calculate : null;
        };
        layout.Controls.Add(actions, 0, 2);

        AddResultColumn(m_Results, () => catalog["SYS_RNG_ADVANCES"], 68);
        var growthSeedColumn = AddResultColumn(m_Results, () => catalog["SYS_GROWTH_SEED"], 110);
        foreach (var stat in m_ViewModel.GrowthRates)
            AddResultColumn(m_Results, () => stat.Name, 42).DefaultCellStyle.Format = "+0;-0;0";
        var nextSeedColumn = AddResultColumn(m_Results, () => catalog["SYS_NEXT_GROWTH_SEED"], 110);
        var seedMenu = new SeedCellMenu(m_Results, m_Text, AdoptSeed);
        components!.Add(seedMenu);
        m_Results.CellContextMenuStripNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == growthSeedColumn.Index || e.ColumnIndex == nextSeedColumn.Index))
                e.ContextMenuStrip = seedMenu;
        };
        m_Results.CellValueNeeded += OnResultCellValueNeeded;
        layout.Controls.Add(m_Results, 0, 3);
        m_Text.Bind(UpdatePredictionMessage);

        void ShowUnitTab()
        {
            seedInput.Text = m_ViewModel.GrowthSeed;
            RefreshPredictionResults();
            tabs.SelectedTab = seedInput.Text.Length == 0 ? search : prediction;
        }

        m_ViewModel.UnitSelected += ShowUnitTab;
        searchPanel.SeedAdopted += AdoptSeed;

        void AdoptSeed(uint seed)
        {
            m_ViewModel.AdoptGrowthSeed(seed);
            ShowUnitTab();
        }
    }

    private MenuStrip CreateMenu(LocalizationCatalog catalog, Action<string> saveLanguage)
    {
        var menu = new MenuStrip();
        var settings = new ToolStripMenuItem();
        var languages = new ToolStripMenuItem();
        menu.Items.Add(settings);
        settings.DropDownItems.Add(languages);
        m_Text.Bind(() => settings.Text = catalog["SYS_SETTINGS"]);
        m_Text.Bind(() => languages.Text = catalog["SYS_LANGUAGE"]);
        foreach (var option in catalog.Languages)
        {
            var item = new ToolStripMenuItem(option.DisplayName);
            languages.DropDownItems.Add(item);
            m_Text.Bind(() => item.Checked = catalog.Code == option.Code);
            item.Click += (_, _) =>
            {
                catalog.Select(option.Code);
                m_Text.Refresh();
                try
                {
                    saveLanguage(option.Code);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    ShowWarning(catalog["SYS_LANGUAGE_SAVE_ERROR"]);
                }
            };
        }
        return menu;
    }

    private void ShowWarning(string message)
    {
        TaskDialog.ShowDialog(this, new TaskDialogPage
        {
            Caption = Text, Text = message, Icon = TaskDialogIcon.Warning,
            Buttons = { new TaskDialogButton(m_Text.Catalog["SYS_OK"]) }
        });
    }

    private static TableLayoutPanel AddTabLayout(TabPage tab)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < layout.RowCount - 1; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);
        return layout;
    }

    private Button BuildMapRandomPage(TabPage page)
    {
        var catalog = m_Text.Catalog;
        var model = new MapRandomViewModel(catalog);
        var layout = AddTabLayout(page);
        layout.Controls.Add(m_Text.Label("SYS_MAP_RNG_INPUT_HINT"), 0, 0);
        var inputs = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        var results = CreateResultsGrid();
        AddResultColumn(results, () => catalog["SYS_RNG_ADVANCES"], 68);
        foreach (var (index, word) in "XYZW".Index())
        {
            string label = word.ToString();
            AddInput(inputs, () => label, model.Words[index], value => model.Words[index] = value);
            AddResultColumn(results, () => label, 90);
        }
        layout.Controls.Add(inputs, 0, 1);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        AddInput(actions, () => catalog["SYS_MAX_RNG_ADVANCES"], model.MaxAdvances, value => model.MaxAdvances = value);
        var calculate = new Button { AutoSize = true };
        var error = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
        m_Text.Bind(() =>
        {
            calculate.Text = catalog["SYS_CALCULATE"];
            results.AccessibleName = catalog["SYS_MAP_RNG_TAB"];
            error.Text = model.ErrorMessage;
        });
        calculate.Click += (_, _) =>
        {
            model.Calculate();
            error.Text = model.ErrorMessage;
            results.RowCount = model.Results.Count;
            results.Invalidate();
        };
        actions.Controls.Add(calculate);
        actions.Controls.Add(error);
        layout.Controls.Add(actions, 0, 2);

        results.CellValueNeeded += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= model.Results.Count)
                return;
            var row = model.Results[e.RowIndex];
            e.Value = e.ColumnIndex switch
            {
                0 => row.Advances, 1 => row.X, 2 => row.Y, 3 => row.Z, 4 => row.W, _ => null
            };
        };
        layout.Controls.Add(results, 0, 3);
        return calculate;
    }

    private TextBox AddInput(FlowLayoutPanel panel, Func<string> caption, string initialValue, Action<string> update)
    {
        var input = new TextBox { Text = initialValue };
        var label = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
        m_Text.Bind(() => label.Text = input.AccessibleName = caption());
        input.TextChanged += (_, _) => update(input.Text);
        panel.Controls.Add(label);
        panel.Controls.Add(input);
        return input;
    }

    private GroupBox CreatePredictionFilters()
    {
        var group = new GroupBox { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
        m_Text.Bind(() => group.Text = m_Text.Catalog["SYS_PREDICTION_FILTER"]);
        int statsPerColumn = (m_ViewModel.GrowthRates.Count + 1) / 2;
        var filters = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 4 };
        for (int i = 0; i < 2; i++)
        {
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
        }
        for (int i = 0; i < m_ViewModel.GrowthRates.Count; i++)
        {
            var stat = m_ViewModel.GrowthRates[i];
            var label = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
            var selector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            m_Text.Bind(() =>
            {
                label.Text = selector.AccessibleName = stat.Name;
                selector.Items.Clear();
                foreach (var option in m_ViewModel.GrowthConditionOptions)
                {
                    int index = selector.Items.Add(option.Name);
                    if (option.Value == stat.Condition)
                        selector.SelectedIndex = index;
                }
            });
            selector.SelectionChangeCommitted += (_, _) =>
                stat.Condition = m_ViewModel.GrowthConditionOptions[selector.SelectedIndex].Value;
            int column = i / statsPerColumn * 2;
            int row = i % statsPerColumn;
            filters.Controls.Add(label, column, row);
            filters.Controls.Add(selector, column + 1, row);
        }
        var counts = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        var countLabel = m_Text.Label("SYS_FILTER_GROWTH_COUNT");
        countLabel.Anchor = AnchorStyles.Left;
        counts.Controls.Add(countLabel);
        AddCount("SYS_FILTER_MIN_GROWTH_COUNT", m_ViewModel.MinGrowthCount, value => m_ViewModel.MinGrowthCount = value);
        counts.Controls.Add(new Label { Text = "～", AutoSize = true, Anchor = AnchorStyles.Left });
        AddCount("SYS_FILTER_MAX_GROWTH_COUNT", m_ViewModel.MaxGrowthCount, value => m_ViewModel.MaxGrowthCount = value);
        filters.Controls.Add(counts, 0, statsPerColumn);
        filters.SetColumnSpan(counts, filters.ColumnCount);
        group.Controls.Add(filters);
        return group;

        void AddCount(string key, int value, Action<int> update)
        {
            var input = new NumericUpDown { Maximum = m_ViewModel.GrowthRates.Count, Value = value, Width = 48 };
            m_Text.Bind(() => input.AccessibleName = m_Text.Catalog[key]);
            input.ValueChanged += (_, _) => update((int)input.Value);
            counts.Controls.Add(input);
        }
    }

    private DataGridViewTextBoxColumn AddResultColumn(DataGridView grid, Func<string> header, int width)
    {
        var column = new DataGridViewTextBoxColumn
        {
            MinimumWidth = width, FillWeight = width,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        m_Text.Bind(() => column.HeaderText = header());
        grid.Columns.Add(column);
        return column;
    }

    private void RefreshPredictionResults()
    {
        UpdatePredictionMessage();
        m_Results.RowCount = m_ViewModel.Results.Count;
        m_Results.Invalidate();
    }

    private void UpdatePredictionMessage() =>
        m_Error.Text = m_ViewModel.ErrorMessage ?? (m_ViewModel.HasCalculated && m_ViewModel.Results.Count == 0
            ? m_Text.Catalog["SYS_PREDICTION_NO_MATCHES"] : "");

    private void OnResultCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        var results = m_ViewModel.Results;
        // 仮想グリッドは行を削除するときにも、直前の選択セルの値を要求する。
        if (e.RowIndex < 0 || e.RowIndex >= results.Count)
            return;
        var row = results[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => row.Advances,
            1 => row.GrowthSeed,
            var column when column == m_Results.ColumnCount - 1 => row.NextGrowthSeed,
            _ => row.StatGains[e.ColumnIndex - 2]
        };
    }
}
