using System.Runtime.InteropServices;
using System.Text;
using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

public sealed partial class MainForm : Form
{
    private readonly WindowText m_Text;
    private readonly MainViewModel m_ViewModel;
    private readonly ToolTip m_CopyToast;
    private readonly Label m_Error = new() { AutoSize = true, ForeColor = SystemColors.WindowText };
    private readonly FlowLayoutPanel m_ExportActions = new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Anchor = AnchorStyles.Right,
        Margin = Padding.Empty, WrapContents = false
    };
    private readonly DataGridView m_Results = new()
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
        m_CopyToast = new ToolTip(components!) { ShowAlways = true };
        m_Text = new WindowText(catalog, components!);
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
        var prediction = new TabPage { AutoScrollMinSize = new Size(0, 475) };
        var search = new TabPage();
        m_Text.Bind(() => prediction.Text = catalog["SYS_PREDICTION_TAB"]);
        m_Text.Bind(() => search.Text = catalog["SYS_SEARCH_TAB"]);
        var searchPanel = new SeedSearchPanel(searchModel, settings, m_Text) { Dock = DockStyle.Fill };
        search.Controls.Add(searchPanel);
        tabs.TabPages.Add(search);
        tabs.TabPages.Add(prediction);
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        content.Controls.Add(tabs);
        content.Controls.Add(settings);
        Controls.Add(content);
        MainMenuStrip = CreateMenu(catalog, saveLanguage);
        Controls.Add(MainMenuStrip);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < layout.RowCount - 1; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        prediction.Controls.Add(layout);

        var inputs = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        var seedInput = AddInput(inputs, "SYS_GROWTH_SEED_INPUT", m_ViewModel.GrowthSeed, value => m_ViewModel.GrowthSeed = value);
        AddInput(inputs, "SYS_MAX_RNG_ADVANCES", m_ViewModel.MaxAdvances, value => m_ViewModel.MaxAdvances = value);
        layout.Controls.Add(inputs, 0, 0);
        layout.Controls.Add(CreatePredictionFilters(), 0, 1);

        var actions = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1 };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var calculate = new Button { AutoSize = true, Anchor = AnchorStyles.Left };
        m_Text.Bind(() => calculate.Text = catalog["SYS_CALCULATE"]);
        calculate.Click += (_, _) =>
        {
            m_ViewModel.Calculate();
            RefreshPredictionResults();
        };
        actions.Controls.Add(calculate, 0, 0);
        m_Error.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        actions.Controls.Add(m_Error, 1, 0);
        var copy = new Button { AutoSize = true };
        var save = new Button { AutoSize = true };
        m_Text.Bind(() => copy.Text = catalog["SYS_COPY"]);
        m_Text.Bind(() => save.Text = catalog["SYS_SAVE"]);
        copy.Click += (_, _) => CopyPredictionCsv(copy);
        save.Click += (_, _) => SavePredictionCsv();
        m_ExportActions.Controls.Add(copy);
        m_ExportActions.Controls.Add(save);
        actions.Controls.Add(m_ExportActions, 2, 0);
        tabs.SelectedIndexChanged += (_, _) => AcceptButton = tabs.SelectedTab == prediction ? calculate : null;
        layout.Controls.Add(actions, 0, 2);

        AddResultColumn(() => catalog["SYS_RNG_ADVANCES"], 68);
        var classColumn = AddResultColumn(() => catalog["SYS_JOB"], 110);
        m_Text.Bind(() => classColumn.MinimumWidth = m_ViewModel.Setup.PredictionClassGroups
            .SelectMany(group => group.Classes)
            .Select(job => TextRenderer.MeasureText(job.Name, m_Results.Font).Width + 4)
            .Append(110).Max());
        var growthSeedColumn = AddResultColumn(() => catalog["SYS_GROWTH_SEED"], 110);
        var examColumn = AddResultColumn(() => catalog["SYS_EXAM"], 42);
        examColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        m_Text.Bind(() => examColumn.MinimumWidth =
            TextRenderer.MeasureText(catalog.Format("SYS_EXAM_MIN_PASS_RATE", 100), m_Results.Font).Width + 4);
        m_Text.BindToolTip(examColumn, "SYS_EXAM_MIN_PASS_RATE_TOOLTIP");
        foreach (var stat in m_ViewModel.GrowthRates)
            AddResultColumn(() => stat.Name, 42).DefaultCellStyle.Format = "+0;-0;0";
        var nextSeedColumn = AddResultColumn(() => catalog["SYS_NEXT_GROWTH_SEED"], 110);
        var seedMenu = new SeedCellMenu(m_Results, m_Text, AdoptSeed);
        components!.Add(seedMenu);
        m_Results.CellContextMenuStripNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == growthSeedColumn.Index || e.ColumnIndex == nextSeedColumn.Index))
                e.ContextMenuStrip = seedMenu;
        };
        m_Results.CellValueNeeded += OnResultCellValueNeeded;
        layout.Controls.Add(m_Results, 0, 3);
        m_Text.Bind(RefreshPredictionResults);

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

    private TextBox AddInput(FlowLayoutPanel panel, string key, string initialValue, Action<string> update)
    {
        var input = new TextBox { Text = initialValue };
        var label = m_Text.Label(key, input);
        label.Anchor = AnchorStyles.Left;
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
        var classFilter = CreateClassFilter();
        filters.Controls.Add(classFilter, 0, statsPerColumn + 1);
        filters.SetColumnSpan(classFilter, filters.ColumnCount);
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

    private FlowLayoutPanel CreateClassFilter()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        var consider = new CheckBox { AutoSize = true, Anchor = AnchorStyles.Left };
        var settings = new Button { AutoSize = true };
        panel.Controls.Add(consider);
        panel.Controls.Add(settings);
        m_Text.Bind(() =>
        {
            consider.Text = m_Text.Catalog["SYS_CONSIDER_CLASS_CHANGES"];
            settings.Text = m_Text.Catalog["SYS_SETTINGS"];
        });
        consider.Click += (_, _) => m_ViewModel.ConsiderClassChanges = consider.Checked;
        settings.Click += (_, _) =>
        {
            using var dialog = new PredictionClassDialog(m_ViewModel, m_Text.Catalog);
            dialog.ShowDialog(this);
        };
        m_ViewModel.UnitSelected += RefreshSelection;
        RefreshSelection();
        return panel;

        void RefreshSelection()
        {
            consider.Enabled = settings.Enabled = !m_ViewModel.Setup.IsManual;
            consider.Checked = consider.Enabled && m_ViewModel.ConsiderClassChanges;
        }
    }

    private DataGridViewTextBoxColumn AddResultColumn(Func<string> header, int width)
    {
        var column = new DataGridViewTextBoxColumn
        {
            MinimumWidth = width, FillWeight = width,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        m_Text.Bind(() => column.HeaderText = header());
        m_Results.Columns.Add(column);
        return column;
    }

    private void RefreshPredictionResults()
    {
        UpdatePredictionMessage();
        m_ExportActions.Enabled = m_ViewModel.HasCalculated;
        m_Results.RowCount = m_ViewModel.Results.Count;
        m_Results.Invalidate();
    }

    private void CopyPredictionCsv(Button copy)
    {
        m_CopyToast.Hide(copy);
        string message;
        int duration;
        try
        {
            Clipboard.SetText(m_ViewModel.CreatePredictionCsv());
            message = m_Text.Catalog["SYS_COPIED"];
            duration = 3000;
        }
        catch (Exception ex) when (ex is ExternalException or UnauthorizedAccessException)
        {
            const int clipboardCannotOpen = unchecked((int)0x800401D0);
            string reason = ex.HResult == clipboardCannotOpen ? m_Text.Catalog["SYS_CLIPBOARD_BUSY"] : ex.Message;
            if (string.IsNullOrWhiteSpace(reason))
                reason = $"0x{ex.HResult:X8}";
            message = m_Text.Catalog["SYS_COPY_ERROR"] + Environment.NewLine + reason;
            duration = 7000;
        }
        m_CopyToast.Show(message, copy, 0, copy.Height, duration);
    }

    private void SavePredictionCsv()
    {
        try
        {
            using var dialog = new SaveFileDialog
            {
                FileName = m_ViewModel.PredictionCsvFileName,
                Filter = m_Text.Catalog["SYS_CSV_FILE"] + " (*.csv)|*.csv",
                DefaultExt = "csv",
                RestoreDirectory = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                File.WriteAllText(dialog.FileName, m_ViewModel.CreatePredictionCsv(), Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException)
        {
            ShowWarning(m_Text.Catalog["SYS_CSV_SAVE_ERROR"]);
        }
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
            1 => m_Text.Catalog.Job(row.ClassId),
            2 => row.GrowthSeed,
            3 => m_Text.Catalog.Format("SYS_EXAM_MIN_PASS_RATE", row.MinimumExamPassRate),
            var column when column == m_Results.ColumnCount - 1 => row.NextGrowthSeed,
            _ => row.StatGains[e.ColumnIndex - 4]
        };
    }
}
