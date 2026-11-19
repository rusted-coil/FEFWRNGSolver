using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

public sealed partial class MainForm : Form
{
    private readonly MainViewModel m_ViewModel = new();
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
        Text = m_ViewModel.Title;
        MinimumSize = new Size(660, 400);
        ClientSize = new Size(920, 600);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 8) };
        var prediction = new TabPage("レベルアップ予測");
        tabs.TabPages.Add(prediction);
        var search = new TabPage("成長seed特定");
        search.Controls.Add(new SeedSearchPanel { Dock = DockStyle.Fill });
        tabs.TabPages.Add(search);
        Controls.Add(tabs);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < layout.RowCount - 1; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        prediction.Controls.Add(layout);
        layout.Controls.Add(new Label { AutoSize = true, Text = "抽選成長　各能力の初期値は0、上限は99、最低保証は2種類です。" }, 0, 0);

        layout.Controls.Add(new GrowthSettingsPanel(m_ViewModel, includePredictionInputs: true), 0, 1);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        var calculate = new Button { Text = "計算", AutoSize = true };
        calculate.Click += (_, _) => Calculate();
        actions.Controls.Add(calculate);
        m_Error.Anchor = AnchorStyles.Left;
        actions.Controls.Add(m_Error);
        AcceptButton = calculate;
        tabs.SelectedIndexChanged += (_, _) => AcceptButton = tabs.SelectedTab == prediction ? calculate : null;
        layout.Controls.Add(actions, 0, 2);

        AddResultColumn("回", 52);
        AddResultColumn("成長seed", 110);
        foreach (var stat in m_ViewModel.GrowthRates)
            AddResultColumn(stat.Name, 42).DefaultCellStyle.Format = "+0;-0;0";
        AddResultColumn("次の成長seed", 110);
        m_Results.CellValueNeeded += OnResultCellValueNeeded;
        layout.Controls.Add(m_Results, 0, 3);
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

    private void Calculate()
    {
        m_ViewModel.Calculate();
        m_Error.Text = m_ViewModel.ErrorMessage;
        m_Results.RowCount = m_ViewModel.Results.Count;
        m_Results.Invalidate();
    }

    private void OnResultCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        var row = m_ViewModel.Results[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => row.Number,
            1 => row.GrowthSeed,
            var column when column == m_Results.ColumnCount - 1 => row.NextGrowthSeed,
            _ => row.StatGains[e.ColumnIndex - 2]
        };
    }
}
