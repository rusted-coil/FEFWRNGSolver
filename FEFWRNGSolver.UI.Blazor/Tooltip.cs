namespace FEFWRNGSolver.UI.Blazor;

internal static class Tooltip
{
    public static Dictionary<string, object> For(string description) => new()
    {
        ["title"] = description,
        ["aria-description"] = description
    };
}
