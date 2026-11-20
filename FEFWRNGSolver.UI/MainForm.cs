using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

public sealed partial class MainForm : Form
{
    private readonly WindowText m_Text;
    private readonly MainViewModel m_ViewModel;
    private readonly Label m_Error = new() { AutoSize = true, ForeColor = SystemColors.WindowText };
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
        ClientSize = new Size(920, 600);

        var settings = new GrowthSettingsPanel(m_ViewModel, m_Text) { TabIndex = 0 };
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 8), TabIndex = 1 };
        var prediction = new TabPage();
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
        layout.Controls.Add(m_Text.Label("SYS_PREDICTION_RULES"), 0, 0);

        var inputs = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 2 };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var seedInput = AddInput(inputs, 0, "SYS_GROWTH_SEED_INPUT", m_ViewModel.GrowthSeed, value => m_ViewModel.GrowthSeed = value);
        AddInput(inputs, 1, "SYS_LEVEL_UP_COUNT", m_ViewModel.LevelUpCount, value => m_ViewModel.LevelUpCount = value);
        layout.Controls.Add(inputs, 0, 1);

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
        tabs.SelectedIndexChanged += (_, _) => AcceptButton = tabs.SelectedTab == prediction ? calculate : null;
        layout.Controls.Add(actions, 0, 2);

        AddResultColumn(() => catalog["SYS_LEVEL_NUMBER"], 52);
        var growthSeedColumn = AddResultColumn(() => catalog["SYS_GROWTH_SEED"], 110);
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
        m_Text.Bind(() => m_Error.Text = m_ViewModel.ErrorMessage);

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

    private TextBox AddInput(TableLayoutPanel panel, int column, string key, string initialValue, Action<string> update)
    {
        var input = new TextBox { Text = initialValue, Dock = DockStyle.Fill };
        input.TextChanged += (_, _) => update(input.Text);
        panel.Controls.Add(m_Text.Label(key, input), column, 0);
        panel.Controls.Add(input, column, 1);
        return input;
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
        m_Error.Text = m_ViewModel.ErrorMessage;
        m_Results.RowCount = m_ViewModel.Results.Count;
        m_Results.Invalidate();
    }

    private void OnResultCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        var results = m_ViewModel.Results;
        // 仮想グリッドは行を削除するときにも、直前の選択セルの値を要求する。
        if (e.RowIndex < 0 || e.RowIndex >= results.Count)
            return;
        var row = results[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => row.Number,
            1 => row.GrowthSeed,
            var column when column == m_Results.ColumnCount - 1 => row.NextGrowthSeed,
            _ => row.StatGains[e.ColumnIndex - 2]
        };
    }
}
