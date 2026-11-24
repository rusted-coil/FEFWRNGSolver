using System.ComponentModel;
using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class WindowText(LocalizationCatalog catalog, IContainer components)
{
    private readonly List<Action> m_Updates = [];
    private ToolTip? m_ControlToolTip;

    public LocalizationCatalog Catalog { get; } = catalog;

    public void Bind(Action update)
    {
        m_Updates.Add(update);
        update();
    }

    public void BindToolTip(DataGridViewColumn column, string key) =>
        Bind(() => column.ToolTipText = Catalog[key]);

    public void BindToolTip(Control control, string key)
    {
        var toolTip = m_ControlToolTip ??= new ToolTip(components);
        Bind(() => toolTip.SetToolTip(control, Catalog[key]));
    }

    public Label Label(string key, Control? input = null)
    {
        var label = new Label { AutoSize = true };
        Bind(() =>
        {
            label.Text = Catalog[key];
            if (input is not null)
                input.AccessibleName = label.Text;
        });
        return label;
    }

    public void Refresh()
    {
        foreach (var update in m_Updates)
            update();
    }
}
