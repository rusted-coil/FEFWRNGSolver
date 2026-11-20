using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class SeedSearchPanel : UserControl
{
    private readonly SeedSearchViewModel m_ViewModel;
    private readonly GrowthSettingsPanel m_Settings;
    private readonly LocalizationCatalog m_Catalog;
    private readonly DataGridView m_Observations = CreateGrid();
    private readonly DataGridView m_Results = CreateGrid();
    private readonly SeedCellMenu m_SeedMenu;
    private readonly Button m_Add = new() { AutoSize = true };
    private readonly Button m_Remove = new() { AutoSize = true };
    private readonly Button m_Search = new() { AutoSize = true };
    private readonly Button m_Cancel = new() { AutoSize = true };
    private readonly Label m_Status = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly ProgressBar m_Progress = new()
    {
        Dock = DockStyle.Top, Maximum = 1000, Visible = false
    };
    private (int Low, int High)[] m_Options = [];

    public event Action<uint>? SeedAdopted;

    public SeedSearchPanel(SeedSearchViewModel viewModel, GrowthSettingsPanel settings, WindowText text)
    {
        m_ViewModel = viewModel;
        m_Settings = settings;
        m_Catalog = text.Catalog;
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

        layout.Controls.Add(text.Label("SYS_OBSERVATION_GUIDE"), 0, 0);
        var observationActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = Padding.Empty };
        observationActions.Controls.AddRange([m_Add, m_Remove]);
        layout.Controls.Add(observationActions, 0, 1);
        m_Observations.VirtualMode = true;
        m_Observations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
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
        var searchActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = Padding.Empty };
        searchActions.Controls.AddRange([m_Search, m_Cancel]);
        layout.Controls.Add(searchActions, 0, 3);
        layout.Controls.Add(m_Status, 0, 4);
        layout.Controls.Add(m_Progress, 0, 5);
        m_Results.ReadOnly = true;
        m_Results.Columns.Add(TextColumn());
        m_Results.Columns.Add(TextColumn());
        m_Results.DefaultCellStyle.Format = "X8";
        m_SeedMenu = new SeedCellMenu(m_Results, text, seed => SeedAdopted?.Invoke(seed));
        layout.Controls.Add(m_Results, 0, 6);

        m_Settings.Changed += () =>
        {
            UpdateObservationColumns();
            m_ViewModel.InputsChanged();
        };
        m_Search.Click += async (_, _) => await m_ViewModel.SearchAsync(work => Task.Run(work));
        m_Cancel.Click += (_, _) => m_ViewModel.Cancel();
        m_ViewModel.Changed += RefreshState;
        m_Observations.Columns.Add(TextColumn());
        foreach (var stat in m_ViewModel.Settings.GrowthRates)
        {
            var column = new DataGridViewCheckBoxColumn
            {
                MinimumWidth = 32,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            text.Bind(() => column.HeaderText = stat.Name);
            m_Observations.Columns.Add(column);
        }
        UpdateObservationColumns();
        text.Bind(() =>
        {
            m_Add.Text = m_Catalog["SYS_ADD_OBSERVATION"];
            m_Remove.Text = m_Catalog["SYS_REMOVE_SELECTED_ROW"];
            m_Search.Text = m_Catalog["SYS_CALCULATE"];
            m_Cancel.Text = m_Catalog["SYS_CANCEL_SEARCH"];
            m_Observations.AccessibleName = m_Catalog["SYS_SEARCH_OBSERVATIONS"];
            m_Observations.Columns[0].HeaderText = m_Catalog["SYS_LEVEL_NUMBER"];
            m_Results.AccessibleName = m_Catalog["SYS_SEARCH_RESULTS"];
            m_Progress.AccessibleName = m_Catalog["SYS_SEARCH_PROGRESS_LABEL"];
            m_Results.Columns[0].HeaderText = m_Catalog["SYS_MATCH_INITIAL"];
            m_Results.Columns[1].HeaderText = m_Catalog["SYS_MATCH_NEXT"];
            RefreshState();
        });
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill, RowHeadersVisible = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
    };

    private static DataGridViewTextBoxColumn TextColumn() => new()
    {
        ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
    };

    private void UpdateObservationColumns()
    {
        var options = m_ViewModel.GetGainOptions();
        if (m_Options.SequenceEqual(options))
            return;

        m_Options = options;
        for (int i = 0; i < options.Length; i++)
        {
            var (low, high) = options[i];
            m_Observations.Columns[i + 1].ReadOnly = low == high;
        }
        m_Observations.Invalidate();
    }

    private void SelectObservation(int row)
    {
        if (row >= 0 && row < m_Observations.RowCount)
            m_Observations.CurrentCell = m_Observations[0, row];
    }

    private void ReadObservation(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= m_ViewModel.Observations.Count)
            return;
        if (e.ColumnIndex == 0)
        {
            e.Value = e.RowIndex + 1;
            return;
        }
        var (low, high) = m_Options[e.ColumnIndex - 1];
        e.Value = low == high ? low > 0 : m_ViewModel.Observations[e.RowIndex][e.ColumnIndex - 1];
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

        m_Status.Text = m_ViewModel.ErrorMessage ?? GetStatus();
        m_Progress.Value = (int)(m_ViewModel.Scanned * m_Progress.Maximum / SeedSearchViewModel.CandidateCount);
        m_Progress.Visible = m_ViewModel.IsRunning || m_ViewModel.HasCompleted || m_ViewModel.WasCancelled;
        var matches = m_ViewModel.Matches;
        if (m_Results.RowCount > matches.Count)
            m_Results.Rows.Clear();
        for (int i = m_Results.RowCount; i < matches.Count; i++)
        {
            int row = m_Results.Rows.Add(matches[i].InitialSeed, matches[i].NextSeed);
            foreach (DataGridViewCell cell in m_Results.Rows[row].Cells)
                cell.ContextMenuStrip = m_SeedMenu;
        }
    }

    private string GetStatus()
    {
        if (!m_ViewModel.IsRunning && !m_ViewModel.HasCompleted && !m_ViewModel.WasCancelled)
            return "";
        string status = m_ViewModel.HasCompleted ? m_Catalog["SYS_SEARCH_COMPLETED"]
            : m_ViewModel.WasCancelled ? m_Catalog["SYS_SEARCH_CANCELLED"]
            : m_ViewModel.WorkerCount == 0 ? m_Catalog["SYS_SEARCH_PREPARING"]
            : m_Catalog.Format("SYS_SEARCH_RUNNING", m_ViewModel.WorkerCount);
        string progress = m_Catalog.Format("SYS_SEARCH_PROGRESS", m_ViewModel.Scanned, SeedSearchViewModel.CandidateCount);
        string matches = m_Catalog.Format("SYS_SEARCH_MATCHES", m_ViewModel.MatchCount);
        string result = string.Join("  ", status, progress, matches);
        if (m_ViewModel.MatchCount > m_ViewModel.Matches.Count)
            result += "  " + m_Catalog.Format("SYS_SEARCH_MATCH_LIMIT", m_ViewModel.Matches.Count);
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_ViewModel.Changed -= RefreshState;
            m_ViewModel.Cancel();
            m_SeedMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
