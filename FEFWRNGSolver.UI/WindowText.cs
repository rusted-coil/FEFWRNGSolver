using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class WindowText(LocalizationCatalog catalog)
{
    private readonly List<Action> m_Updates = [];

    public LocalizationCatalog Catalog { get; } = catalog;

    public void Bind(Action update)
    {
        m_Updates.Add(update);
        update();
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
