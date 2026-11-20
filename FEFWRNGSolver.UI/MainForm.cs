using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

public sealed partial class MainForm : Form
{
    private readonly MainViewModel m_ViewModel;
    private readonly Label m_Error = new() { AutoSize = true, ForeColor = SystemColors.WindowText };
    private readonly DataGridView m_Results = new()
    {
        Dock = DockStyle.Fill,
        AccessibleName = "抽選成長の計算結果",
        ReadOnly = true,
        VirtualMode = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
    };

    public MainForm()
    {
        InitializeComponent();
        var store = new FileUnitSettingsStore(message =>
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning));
        var searchModel = new SeedSearchViewModel(store);
        m_ViewModel = searchModel.Settings;
        Text = m_ViewModel.Title;
        MinimumSize = new Size(660, 400);
        ClientSize = new Size(920, 600);

        var settings = new GrowthSettingsPanel(m_ViewModel) { TabIndex = 0 };
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 8), TabIndex = 1 };
        var prediction = new TabPage("レベルアップ予測");
        var search = new TabPage("成長seed特定");
        var searchPanel = new SeedSearchPanel(searchModel, settings) { Dock = DockStyle.Fill };
        search.Controls.Add(searchPanel);
        tabs.TabPages.Add(search);
        tabs.TabPages.Add(prediction);
        Controls.Add(tabs);
        Controls.Add(settings);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < layout.RowCount - 1; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        prediction.Controls.Add(layout);
        layout.Controls.Add(new Label { AutoSize = true, Text = "抽選成長　各能力の初期値は0、上限は99、最低保証は2種類です。" }, 0, 0);

        var inputs = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 2 };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        var seedInput = AddInput(inputs, 0, "成長seed（16進数8桁）", m_ViewModel.GrowthSeed, value => m_ViewModel.GrowthSeed = value);
        AddInput(inputs, 1, "レベルアップ回数", m_ViewModel.LevelUpCount, value => m_ViewModel.LevelUpCount = value);
        layout.Controls.Add(inputs, 0, 1);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        var calculate = new Button { Text = "計算", AutoSize = true };
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

        AddResultColumn("回", 52);
        var growthSeedColumn = AddResultColumn("成長seed", 110);
        foreach (var stat in m_ViewModel.GrowthRates)
            AddResultColumn(stat.Name, 42).DefaultCellStyle.Format = "+0;-0;0";
        var nextSeedColumn = AddResultColumn("次の成長seed", 110);
        var seedMenu = new SeedCellMenu(m_Results, AdoptSeed);
        components!.Add(seedMenu);
        m_Results.CellContextMenuStripNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == growthSeedColumn.Index || e.ColumnIndex == nextSeedColumn.Index))
                e.ContextMenuStrip = seedMenu;
        };
        m_Results.CellValueNeeded += OnResultCellValueNeeded;
        layout.Controls.Add(m_Results, 0, 3);

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

    private static TextBox AddInput(TableLayoutPanel panel, int column, string name, string initialValue, Action<string> update)
    {
        var input = new TextBox { AccessibleName = name, Text = initialValue, Dock = DockStyle.Fill };
        input.TextChanged += (_, _) => update(input.Text);
        panel.Controls.Add(new Label { Text = name, AutoSize = true }, column, 0);
        panel.Controls.Add(input, column, 1);
        return input;
    }

    private DataGridViewTextBoxColumn AddResultColumn(string header, int width)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header, MinimumWidth = width, FillWeight = width,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
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
