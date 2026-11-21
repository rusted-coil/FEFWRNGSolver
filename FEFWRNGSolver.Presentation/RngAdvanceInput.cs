using System.Globalization;

namespace FEFWRNGSolver.Presentation;

internal static class RngAdvanceInput
{
    public const string DefaultMaximum = "100";

    public static bool TryParseMaximum(string value, out int maximum) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out maximum)
        && maximum is >= 0 and <= 10_000;
}
