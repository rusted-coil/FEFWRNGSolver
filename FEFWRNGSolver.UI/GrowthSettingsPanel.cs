using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class GrowthSettingsPanel : GroupBox
{
    private readonly MainViewModel m_ViewModel;
    private readonly WindowText m_Text;
    private readonly List<TextBox> m_RateInputs = [];
    private readonly TextBox m_GuaranteedStatCount = new() { Width = 52, Anchor = AnchorStyles.Left };
    private bool m_Refreshing;

    public event Action? Changed;

    public GrowthSettingsPanel(MainViewModel viewModel, WindowText text)
    {
        m_ViewModel = viewModel;
        m_Text = text;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.Top;
        m_Text.Bind(() => Text = m_Text.Catalog["SYS_UNIT"]);
        Padding = new Padding(8, 3, 8, 5);
        var layout = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        var selectors = CreateSetupSelectors();
        layout.Controls.Add(selectors, 0, 0);
        layout.SetColumnSpan(selectors, 2);
        layout.Controls.Add(m_Text.Label("SYS_GROWTH_RATES"), 0, 1);

        var rates = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 * viewModel.GrowthRates.Count, RowCount = 1 };
        foreach (var stat in viewModel.GrowthRates)
        {
            rates.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            var label = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
            rates.Controls.Add(label);
            var input = new TextBox { Text = stat.Rate, Dock = DockStyle.Fill };
            m_Text.Bind(() => label.Text = input.AccessibleName = stat.Name);
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
        layout.Controls.Add(m_Text.Label("SYS_GUARANTEED_STAT_COUNT", m_GuaranteedStatCount), 1, 1);
        layout.Controls.Add(m_GuaranteedStatCount, 1, 2);
        RefreshRates();
        m_GuaranteedStatCount.TextChanged += (_, _) =>
        {
            if (!m_Refreshing)
            {
                m_ViewModel.GuaranteedStatCount = m_GuaranteedStatCount.Text;
                Changed?.Invoke();
            }
        };
    }

    private TableLayoutPanel CreateSetupSelectors()
    {
        var setup = m_ViewModel.Setup;
        var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2 };
        var unit = AddSelector(panel, 0, "SYS_UNIT", 20);
        var job = AddSelector(panel, 1, "SYS_JOB", 23);
        var animal = AddSelector(panel, 2, "SYS_ANIMAL", 25);
        var affection = AddSelector(panel, 3, "SYS_AFFECTION", 10);
        var skill = AddSelector(panel, 4, "SYS_SKILL_BONUS", 22);
        var partThree = new CheckBox { AutoSize = true, Anchor = AnchorStyles.Left };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(partThree, 5, 1);
        partThree.Click += (_, _) =>
        {
            m_ViewModel.IsPartThree = partThree.Checked;
            RefreshRates();
        };
        skill.DisplayMember = nameof(SkillBonusOption.Name);
        affection.Items.AddRange(GrowthSetupViewModel.AffectionLevels.Cast<object>().ToArray());
        m_Text.Bind(() =>
        {
            partThree.Text = m_Text.Catalog["SYS_PART_THREE"];
            SetOptions(unit, setup.Units, setup.UnitId);
            RefreshSelections();
        });

        void RefreshSelections()
        {
            partThree.Enabled = m_ViewModel.CanSelectPartThree;
            partThree.Checked = m_ViewModel.IsPartThree;
            SetOptions(job, setup.Classes, setup.ClassId);
            SetOptions(animal, setup.Animals, setup.AnimalId);
            job.Enabled = !setup.IsManual;
            animal.Enabled = !setup.IsManual && setup.Animals.Count > 1;
            affection.Enabled = setup.HasAnimal;
            affection.SelectedItem = setup.Affection;
            var skillOptions = m_ViewModel.SkillOptions;
            skill.Items.Clear();
            skill.Items.AddRange(skillOptions.ToArray());
            skill.SelectedItem = skillOptions.First(option => option.Value == m_ViewModel.SelectedSkillBonus);
            skill.Enabled = skillOptions.Count > 1;
        }

        // 候補の差し替えでは発火しないイベントを使い、利用者の変更だけを反映する。
        unit.SelectionChangeCommitted += (_, _) =>
        {
            setup.UnitId = ((GrowthOption)unit.SelectedItem!).Id;
            RefreshSelections();
            RefreshRates();
        };
        job.SelectionChangeCommitted += (_, _) =>
        {
            setup.ClassId = ((GrowthOption)job.SelectedItem!).Id;
            RefreshSelections();
            RefreshRates();
        };
        animal.SelectionChangeCommitted += (_, _) =>
        {
            setup.AnimalId = ((GrowthOption)animal.SelectedItem!).Id;
            RefreshSelections();
            RefreshRates();
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
        return panel;
    }

    private ComboBox AddSelector(TableLayoutPanel panel, int column, string key, int widthPercent)
    {
        var selector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, widthPercent));
        panel.Controls.Add(m_Text.Label(key, selector), column, 0);
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
        m_GuaranteedStatCount.ReadOnly = !m_ViewModel.Setup.IsManual;
        m_GuaranteedStatCount.Text = m_ViewModel.GuaranteedStatCount;
        m_Refreshing = false;
        Changed?.Invoke();
    }
}
