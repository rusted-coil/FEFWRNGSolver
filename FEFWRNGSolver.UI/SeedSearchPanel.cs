using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class SeedSearchPanel : UserControl
{
    private readonly SeedSearchViewModel m_ViewModel;
    private readonly GrowthSettingsPanel m_Settings;
    private readonly DataGridView m_Observations = CreateGrid("レベルアップごとの観測結果");
    private readonly DataGridView m_Results = CreateGrid("成長seedの候補");
    private readonly Button m_Add = new() { AutoSize = true, Text = "観測を追加" };
    private readonly Button m_Remove = new() { AutoSize = true, Text = "選択行を削除" };
    private readonly Button m_Search = new() { AutoSize = true, Text = "計算" };
    private readonly Button m_Cancel = new() { AutoSize = true, Text = "中止" };
    private readonly Label m_Status = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly ProgressBar m_Progress = new()
    {
        AccessibleName = "探索の進捗", Dock = DockStyle.Top, Maximum = 1000, Visible = false
    };
    private (int Low, int High)[] m_Options = [];

    public SeedSearchPanel(SeedSearchViewModel viewModel, GrowthSettingsPanel settings)
    {
        m_ViewModel = viewModel;
        m_Settings = settings;
        AutoScroll = true;
        AutoScrollMinSize = new Size(0, 360);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7,
            MinimumSize = AutoScrollMinSize, Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            AutoSize = true, Text = "同じ成長条件で連続した抽選成長を入力してください。最低保証2種類、全能力が上限未達、途中の別のseed消費なしとして探索します。"
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            AutoSize = true, Text = "観測結果　各能力の上昇量を入力します。見出しの右の値ならチェックしてください。"
        }, 0, 1);
        m_Observations.VirtualMode = true;
        m_Observations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        m_Observations.MultiSelect = false;
        m_Observations.EditMode = DataGridViewEditMode.EditOnEnter;
        m_Observations.CellValueNeeded += ReadObservation;
        m_Observations.CellValuePushed += WriteObservation;
        m_Observations.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (m_Observations.IsCurrentCellDirty)
                m_Observations.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        layout.Controls.Add(m_Observations, 0, 2);
        m_Add.Click += (_, _) =>
        {
            m_ViewModel.AddObservation();
            SelectObservation(m_ViewModel.Observations.Count - 1);
        };
        m_Remove.Click += (_, _) =>
        {
            if (m_Observations.CurrentCell is { } cell)
            {
                int row = cell.RowIndex;
                m_ViewModel.RemoveObservation(row);
                SelectObservation(Math.Min(row, m_ViewModel.Observations.Count - 1));
            }
        };
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        actions.Controls.AddRange([m_Add, m_Remove, m_Search, m_Cancel]);
        layout.Controls.Add(actions, 0, 3);
        layout.Controls.Add(m_Status, 0, 4);
        layout.Controls.Add(m_Progress, 0, 5);
        m_Results.ReadOnly = true;
        m_Results.Columns.Add(TextColumn("最初の観測直前の成長seed"));
        m_Results.Columns.Add(TextColumn("最後の観測後の次の成長seed"));
        layout.Controls.Add(m_Results, 0, 6);

        m_Settings.Changed += () =>
        {
            UpdateObservationColumns();
            m_ViewModel.InputsChanged();
        };
        m_Search.Click += async (_, _) => await m_ViewModel.SearchAsync(work => Task.Run(work));
        m_Cancel.Click += (_, _) => m_ViewModel.Cancel();
        m_ViewModel.Changed += RefreshState;
        UpdateObservationColumns();
        RefreshState();
    }

    private static DataGridView CreateGrid(string accessibleName) => new()
    {
        AccessibleName = accessibleName, Dock = DockStyle.Fill, RowHeadersVisible = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
    };

    private static DataGridViewTextBoxColumn TextColumn(string header) => new()
    {
        HeaderText = header, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
    };

    private void UpdateObservationColumns()
    {
        var stats = m_ViewModel.Settings.GrowthRates;
        var options = m_ViewModel.GetGainOptions();
        if (m_Options.SequenceEqual(options))
            return;

        int row = m_Observations.CurrentCell?.RowIndex ?? 0;
        m_Options = options;
        m_Observations.Columns.Clear();
        m_Observations.Columns.Add(TextColumn("回"));
        for (int i = 0; i < options.Length; i++)
        {
            var (low, high) = options[i];
            DataGridViewColumn column = low == high ? TextColumn(stats[i].Name) : new DataGridViewCheckBoxColumn
            {
                HeaderText = $"{stats[i].Name}\n+{low} / +{high}",
                ToolTipText = $"未チェックで+{low}、チェックで+{high}",
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            m_Observations.Columns.Add(column);
        }
        m_Observations.RowCount = m_ViewModel.Observations.Count;
        SelectObservation(row);
    }

    private void SelectObservation(int row)
    {
        if (row >= 0 && row < m_Observations.RowCount)
            m_Observations.CurrentCell = m_Observations[0, row];
    }

    private void ReadObservation(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.ColumnIndex == 0)
        {
            e.Value = e.RowIndex + 1;
            return;
        }
        var (low, high) = m_Options[e.ColumnIndex - 1];
        e.Value = low == high ? $"+{low}" : m_ViewModel.Observations[e.RowIndex][e.ColumnIndex - 1];
    }

    private void WriteObservation(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.ColumnIndex > 0 && e.Value is bool gained)
        {
            m_ViewModel.Observations[e.RowIndex][e.ColumnIndex - 1] = gained;
            m_ViewModel.InputsChanged();
        }
    }

    private void RefreshState()
    {
        bool editable = !m_ViewModel.IsRunning;
        m_Settings.Enabled = m_Observations.Enabled = m_Add.Enabled = m_Search.Enabled = editable;
        m_Remove.Enabled = editable && m_ViewModel.Observations.Count > 0;
        m_Cancel.Enabled = !editable;
        if (m_Observations.RowCount != m_ViewModel.Observations.Count)
        {
            m_Observations.RowCount = m_ViewModel.Observations.Count;
            m_Observations.Invalidate();
        }

        string status = m_ViewModel.HasCompleted ? "探索完了" : m_ViewModel.WasCancelled ? "中止" : "探索中";
        if (m_ViewModel.IsRunning && m_ViewModel.WorkerCount > 0)
            status += $"（並列数 {m_ViewModel.WorkerCount}）";
        m_Status.Text = m_ViewModel.ErrorMessage ?? (editable && !m_ViewModel.HasCompleted && !m_ViewModel.WasCancelled
            ? "" : $"{status}　{m_ViewModel.Scanned:N0} / {SeedSearchViewModel.CandidateCount:N0} 調査済み　候補 {m_ViewModel.MatchCount:N0} 件");
        if (m_ViewModel.MatchCount > m_ViewModel.Matches.Count)
            m_Status.Text += $"（先頭{m_ViewModel.Matches.Count}件を表示）";
        m_Progress.Value = (int)(m_ViewModel.Scanned * m_Progress.Maximum / SeedSearchViewModel.CandidateCount);
        m_Progress.Visible = m_ViewModel.IsRunning || m_ViewModel.HasCompleted || m_ViewModel.WasCancelled;
        var matches = m_ViewModel.Matches;
        if (m_Results.RowCount > matches.Count)
            m_Results.Rows.Clear();
        for (int i = m_Results.RowCount; i < matches.Count; i++)
            m_Results.Rows.Add(matches[i].InitialSeed.ToString("X8"), matches[i].NextSeed.ToString("X8"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_ViewModel.Changed -= RefreshState;
            m_ViewModel.Cancel();
        }
        base.Dispose(disposing);
    }
}
