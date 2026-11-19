using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class GrowthSettingsPanel : GroupBox
{
    private readonly MainViewModel m_ViewModel;
    private readonly List<TextBox> m_RateInputs = [];
    private bool m_Refreshing;

    public event Action? Changed;

    public GrowthSettingsPanel(MainViewModel viewModel)
    {
        m_ViewModel = viewModel;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.Top;
        Text = "ユニット";
        Padding = new Padding(8, 3, 8, 5);
        var layout = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        layout.Controls.Add(CreateSetupSelectors(), 0, 0);
        layout.Controls.Add(new Label { Text = "実効成長率（％）", AutoSize = true }, 0, 1);

        var rates = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 * viewModel.GrowthRates.Count, RowCount = 1 };
        foreach (var stat in viewModel.GrowthRates)
        {
            rates.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            rates.Controls.Add(new Label { Text = stat.Name, AutoSize = true, Anchor = AnchorStyles.Left });
            var input = new TextBox { AccessibleName = stat.Name, Text = stat.Rate, Dock = DockStyle.Fill };
            input.TextChanged += (_, _) =>
            {
                if (!m_Refreshing)
                {
                    stat.Rate = input.Text;
                    Changed?.Invoke();
                }
            };
            rates.Controls.Add(input);
            m_RateInputs.Add(input);
        }
        layout.Controls.Add(rates, 0, 2);
    }

    private TableLayoutPanel CreateSetupSelectors()
    {
        var setup = m_ViewModel.Setup;
        var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2 };
        var unit = AddSelector(panel, 0, "ユニット", 20);
        var job = AddSelector(panel, 1, "兵種", 23);
        var animal = AddSelector(panel, 2, "動物", 25);
        var affection = AddSelector(panel, 3, "友好度", 10);
        var skill = AddSelector(panel, 4, "スキル補正", 22);
        skill.DisplayMember = nameof(SkillBonusOption.Name);
        skill.Items.AddRange(MainViewModel.SkillBonusOptions.ToArray());
        skill.SelectedItem = MainViewModel.SkillBonusOptions.First(option => option.Value == m_ViewModel.SelectedSkillBonus);
        SetOptions(unit, setup.Units, setup.UnitId);
        affection.Items.AddRange(GrowthSetupViewModel.AffectionLevels.Cast<object>().ToArray());

        void RefreshSelections()
        {
            SetOptions(job, setup.Classes, setup.ClassId);
            SetOptions(animal, setup.Animals, setup.AnimalId);
            job.Enabled = !setup.IsManual;
            animal.Enabled = !setup.IsManual && setup.Animals.Count > 1;
            affection.Enabled = setup.HasAnimal;
            affection.SelectedItem = setup.Affection;
            RefreshRates();
        }

        // 候補の差し替えでは発火しないイベントを使い、利用者の変更だけを反映する。
        unit.SelectionChangeCommitted += (_, _) =>
        {
            setup.UnitId = ((GrowthOption)unit.SelectedItem!).Id;
            RefreshSelections();
        };
        job.SelectionChangeCommitted += (_, _) =>
        {
            setup.ClassId = ((GrowthOption)job.SelectedItem!).Id;
            RefreshSelections();
        };
        animal.SelectionChangeCommitted += (_, _) =>
        {
            setup.AnimalId = ((GrowthOption)animal.SelectedItem!).Id;
            RefreshSelections();
        };
        affection.SelectionChangeCommitted += (_, _) =>
        {
            setup.Affection = (int)affection.SelectedItem!;
            RefreshRates();
        };
        skill.SelectionChangeCommitted += (_, _) =>
        {
            m_ViewModel.SelectedSkillBonus = ((SkillBonusOption)skill.SelectedItem!).Value;
            RefreshRates();
        };
        RefreshSelections();
        return panel;
    }

    private static ComboBox AddSelector(TableLayoutPanel panel, int column, string name, int widthPercent)
    {
        var selector = new ComboBox { AccessibleName = name, DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, widthPercent));
        panel.Controls.Add(new Label { AutoSize = true, Text = name }, column, 0);
        panel.Controls.Add(selector, column, 1);
        return selector;
    }

    private static void SetOptions(ComboBox selector, IReadOnlyList<GrowthOption> options, string selectedId)
    {
        selector.BeginUpdate();
        selector.Items.Clear();
        selector.DisplayMember = nameof(GrowthOption.Name);
        foreach (var option in options)
        {
            int index = selector.Items.Add(option);
            if (option.Id == selectedId)
                selector.SelectedIndex = index;
        }
        selector.EndUpdate();
    }

    private void RefreshRates()
    {
        m_Refreshing = true;
        for (int i = 0; i < m_RateInputs.Count; i++)
            m_RateInputs[i].Text = m_ViewModel.GrowthRates[i].Rate;
        m_Refreshing = false;
        Changed?.Invoke();
    }
}
