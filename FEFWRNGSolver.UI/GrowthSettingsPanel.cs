using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class GrowthSettingsPanel : TableLayoutPanel
{
    private readonly MainViewModel m_ViewModel;
    private readonly Label m_RatesLabel = new() { AutoSize = true };
    private readonly List<TextBox> m_RateInputs = [];
    private bool m_Refreshing;

    public event Action? Changed;

    public GrowthSettingsPanel(MainViewModel viewModel, bool includePredictionInputs = false)
    {
        m_ViewModel = viewModel;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.Top;
        Margin = Padding.Empty;
        ColumnCount = 1;
        RowCount = 4;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < RowCount; i++)
            RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var settings = new TableLayoutPanel { AutoSize = true, ColumnCount = includePredictionInputs ? 3 : 1, RowCount = 2 };
        if (includePredictionInputs)
        {
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            AddInput(settings, 0, "成長seed（16進数8桁）", m_ViewModel.GrowthSeed, value => m_ViewModel.GrowthSeed = value);
            AddInput(settings, 1, "レベルアップ回数", m_ViewModel.LevelUpCount, value => m_ViewModel.LevelUpCount = value);
        }
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        var skillBonus = new ComboBox
        {
            AccessibleName = "スキル補正", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill,
            DisplayMember = nameof(SkillBonusOption.Name), DataSource = MainViewModel.SkillBonusOptions.ToArray()
        };
        skillBonus.SelectedIndexChanged += (_, _) =>
        {
            if (skillBonus.SelectedItem is SkillBonusOption option)
            {
                m_ViewModel.SelectedSkillBonus = option.Value;
                RefreshRates();
            }
        };
        settings.Controls.Add(new Label { Text = "スキル補正", AutoSize = true }, settings.ColumnCount - 1, 0);
        settings.Controls.Add(skillBonus, settings.ColumnCount - 1, 1);
        Controls.Add(settings, 0, 0);

        Controls.Add(m_RatesLabel, 0, 2);
        var rates = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = m_ViewModel.GrowthRates.Count, RowCount = 2 };
        for (int i = 0; i < rates.ColumnCount; i++)
        {
            var stat = m_ViewModel.GrowthRates[i];
            rates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / rates.ColumnCount));
            m_RateInputs.Add(AddInput(rates, i, stat.Name, stat.Rate, value =>
            {
                if (!m_Refreshing && m_ViewModel.Setup.IsManual)
                {
                    stat.Rate = value;
                    Changed?.Invoke();
                }
            }));
        }
        Controls.Add(rates, 0, 3);
        Controls.Add(CreateSetupSelectors(), 0, 1);
    }

    private TableLayoutPanel CreateSetupSelectors()
    {
        var setup = m_ViewModel.Setup;
        var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 4, RowCount = 2 };
        var unit = AddSelector(panel, 0, "ユニット", 26);
        var job = AddSelector(panel, 1, "兵種", 30);
        var animal = AddSelector(panel, 2, "動物", 31);
        var affection = AddSelector(panel, 3, "友好度", 13);
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
        bool manual = m_ViewModel.Setup.IsManual;
        m_RatesLabel.Text = manual ? "スキル補正前の成長率（％）" : "実効成長率（％）";
        var rates = manual ? default : m_ViewModel.SelectedGrowthRates;
        m_Refreshing = true;
        for (int i = 0; i < m_RateInputs.Count; i++)
        {
            m_RateInputs[i].ReadOnly = !manual;
            m_RateInputs[i].Text = manual ? m_ViewModel.GrowthRates[i].Rate : rates[i].ToString();
        }
        m_Refreshing = false;
        Changed?.Invoke();
    }

    private static TextBox AddInput(TableLayoutPanel panel, int column, string name, string initialValue, Action<string> update)
    {
        var input = new TextBox { AccessibleName = name, Text = initialValue, Dock = DockStyle.Fill };
        input.TextChanged += (_, _) => update(input.Text);
        panel.Controls.Add(new Label { Text = name, AutoSize = true }, column, 0);
        panel.Controls.Add(input, column, 1);
        return input;
    }
}
