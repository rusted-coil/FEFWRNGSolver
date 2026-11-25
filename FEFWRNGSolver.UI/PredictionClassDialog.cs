using FEFWRNGSolver.Domain;
using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class PredictionClassDialog : Form
{
    public PredictionClassDialog(MainViewModel viewModel, LocalizationCatalog text)
    {
        Text = text["SYS_CONSIDER_CLASS_CHANGES"];
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;

        var columns = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
            Padding = new Padding(6)
        };
        foreach (var group in viewModel.Setup.PredictionClassGroups)
        {
            var column = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                FlowDirection = FlowDirection.TopDown, AccessibleName = group.Name, AccessibleRole = AccessibleRole.Grouping
            };
            column.Controls.Add(new Label { AutoSize = true, Text = group.Name, Margin = new Padding(3, 3, 3, 7) });
            foreach (var job in group.Classes)
            {
                var toggle = new CheckBox
                {
                    AutoSize = true, Text = job.Name, UseMnemonic = false, Enabled = job.IsAvailable,
                    Checked = viewModel.IsPredictionClassSelected(job.Id)
                };
                toggle.CheckedChanged += (_, _) => viewModel.SetPredictionClassSelected(job.Id, toggle.Checked);
                column.Controls.Add(toggle);
                AddClassSettings(column, toggle, job, viewModel, text);
            }
            columns.Controls.Add(column);
        }

        var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        viewport.Controls.Add(columns);
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(6)
        };
        var done = new Button { Text = text["SYS_OK"], AutoSize = true, DialogResult = DialogResult.OK };
        footer.Controls.Add(done);
        AcceptButton = CancelButton = done;
        Controls.Add(viewport);
        Controls.Add(footer);

        Load += (_, _) =>
        {
            var content = columns.PreferredSize;
            ClientSize = new Size(content.Width + SystemInformation.VerticalScrollBarWidth,
                content.Height + footer.PreferredSize.Height + SystemInformation.HorizontalScrollBarHeight);
            var available = Screen.FromControl(Owner ?? this).WorkingArea;
            Size = new Size(Math.Min(Width, available.Width), Math.Min(Height, available.Height));
        };
    }

    private static void AddClassSettings(FlowLayoutPanel column, CheckBox toggle, PredictionClassOption job,
        MainViewModel viewModel, LocalizationCatalog text)
    {
        var settings = viewModel.GetPredictionSettings(job.Id);
        var inherit = text["SYS_USE_UNIT_SETTINGS"];
        var skills = viewModel.GetPredictionSkillOptions(job.Id);
        if (skills.Count > 1)
        {
            AddSelector<GrowthSkillBonus?>(AddRow(), job.Name, text["SYS_SKILL_BONUS"],
                [(null, inherit), .. skills.Select(skill => ((GrowthSkillBonus?)skill.Value, skill.Name))],
                settings.SkillBonus, value =>
                    viewModel.SetPredictionSettings(job.Id, viewModel.GetPredictionSettings(job.Id) with { SkillBonus = value }));
        }

        var animals = viewModel.GetPredictionAnimalOptions(job.Id);
        if (animals.Count > 1)
        {
            var row = AddRow();
            var affectionField = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty
            };
            var affection = AddSelector<int>(affectionField, job.Name, text["SYS_AFFECTION"],
                [.. GrowthSetupViewModel.AffectionLevels.Select(level => (level, level.ToString(text.Culture)))],
                settings.Affection, value =>
                    viewModel.SetPredictionSettings(job.Id, viewModel.GetPredictionSettings(job.Id) with { Affection = value }));
            AddSelector<string?>(row, job.Name, text["SYS_ANIMAL"],
                [(null, inherit), .. animals.Select(animal => ((string?)animal.Id, animal.Name))], settings.AnimalId, id =>
                {
                    viewModel.SetPredictionSettings(job.Id, viewModel.GetPredictionSettings(job.Id) with { AnimalId = id });
                    affection.SelectedItem = viewModel.GetPredictionSettings(job.Id).Affection.ToString(text.Culture);
                    affectionField.Visible = !string.IsNullOrEmpty(id);
                });
            row.Controls.Add(affectionField);
            affectionField.Visible = !string.IsNullOrEmpty(settings.AnimalId);
        }

        FlowLayoutPanel AddRow()
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                Margin = new Padding(18, 0, 3, 3), Visible = job.IsAvailable && toggle.Checked
            };
            toggle.CheckedChanged += (_, _) => row.Visible = job.IsAvailable && toggle.Checked;
            column.Controls.Add(row);
            return row;
        }
    }

    private static ComboBox AddSelector<T>(FlowLayoutPanel row, string jobName, string caption,
        (T Value, string Name)[] options, T selected, Action<T> commit)
    {
        var label = new Label { AutoSize = true, Anchor = AnchorStyles.Left, UseMnemonic = false, Text = caption };
        var selector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = $"{jobName} {caption}"
        };
        string[] names = [.. options.Select(option => option.Name)];
        selector.Items.AddRange(names);
        selector.Width = names.Max(name => TextRenderer.MeasureText(name, selector.Font).Width)
            + SystemInformation.VerticalScrollBarWidth + 10;
        selector.SelectedIndex = Array.FindIndex(options, option => EqualityComparer<T>.Default.Equals(option.Value, selected));
        selector.SelectionChangeCommitted += (_, _) => commit(options[selector.SelectedIndex].Value);
        row.Controls.Add(label);
        row.Controls.Add(selector);
        return selector;
    }
}
