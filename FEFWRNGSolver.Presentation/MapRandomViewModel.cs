using System.Globalization;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed class MapRandomViewModel
{
    private readonly LocalizationCatalog m_Text;
    private string? m_InvalidField;

    public MapRandomViewModel(LocalizationCatalog text)
    {
        ArgumentNullException.ThrowIfNull(text);
        m_Text = text;
    }

    public string[] Words { get; } = ["", "", "", ""];
    public string MaxAdvances { get; set; } = RngAdvanceInput.DefaultMaximum;
    public IReadOnlyList<MapRandomRow> Results { get; private set; } = [];
    public string? ErrorMessage => m_InvalidField switch
    {
        null => null,
        nameof(MaxAdvances) => m_Text["SYS_INVALID_MAX_RNG_ADVANCES"],
        _ => m_Text.Format("SYS_INVALID_MAP_RNG_WORD", m_InvalidField)
    };

    public bool Calculate()
    {
        Span<uint> words = stackalloc uint[4];
        for (int i = 0; i < words.Length; i++)
        {
            var digits = Words[i].AsSpan().Trim();
            if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                digits = digits[2..];
            if (digits.Length is < 1 or > 8 ||
                !uint.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out words[i]))
            {
                m_InvalidField = "XYZW"[i].ToString();
                return false;
            }
        }
        if (!RngAdvanceInput.TryParseMaximum(MaxAdvances, out int maximum))
        {
            m_InvalidField = nameof(MaxAdvances);
            return false;
        }

        var rows = new List<MapRandomRow>(maximum + 1);
        var state = new MapRandomState(words[0], words[1], words[2], words[3]);
        for (int advances = 0; advances <= maximum; advances++)
        {
            rows.Add(new(advances, state.X.ToString("X8"), state.Y.ToString("X8"),
                state.Z.ToString("X8"), state.W.ToString("X8")));
            state = state.Next();
        }
        Results = rows.AsReadOnly();
        m_InvalidField = null;
        return true;
    }
}

public sealed record MapRandomRow(int Advances, string X, string Y, string Z, string W);
