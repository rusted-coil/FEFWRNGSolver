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
}
