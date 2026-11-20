using System.Globalization;
using System.Text.Json;

namespace FEFWRNGSolver.Presentation;

public sealed record LanguageOption(string Code, string DisplayName, IReadOnlyList<string> MatchCodes);

public sealed class LocalizationCatalog
{
    private static readonly string[] m_Sections = ["system", "character", "job", "skill"];
    private static readonly LanguageOption[] m_DefaultLanguages =
        [new("jp", "日本語", ["ja"]), new("en", "English", ["en"])];
    private static readonly StringComparer m_CodeComparer = StringComparer.OrdinalIgnoreCase;
    private readonly Dictionary<string, Dictionary<(string Section, string Id), string>> m_Packs = new(m_CodeComparer);

    private LocalizationCatalog(IReadOnlyList<LanguageOption> languages) => Languages = languages;

    public IReadOnlyList<LanguageOption> Languages { get; }
    public string Code { get; private set; } = "";
    public CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

    // Keep the stand-alone view model constructors usable without a platform-specific file reader.
    private static readonly Lazy<LocalizationCatalog> m_Default = new(() =>
    {
        var catalog = new LocalizationCatalog(m_DefaultLanguages);
        var pack = new Dictionary<(string, string), string>();
        foreach (var section in m_Sections)
        {
            using var stream = typeof(LocalizationCatalog).Assembly.GetManifestResourceStream($"Localization.jp.{section}.json")!;
            using var reader = new StreamReader(stream);
            AddSection(pack, section, reader.ReadToEnd());
        }
        catalog.m_Packs.Add("jp", pack);
        catalog.Select("jp");
        return catalog;
    });
    internal static LocalizationCatalog Default => m_Default.Value;

    public static async Task<LocalizationCatalog> LoadAsync(Func<string, Task<string?>> readFile,
        string? savedCode, IEnumerable<string> preferredLanguages)
    {
        var catalog = new LocalizationCatalog(ReadLanguages(await readFile("languages.json").ConfigureAwait(false)));
        var codes = catalog.Languages.Select(language => language.Code).Concat(["en", "jp"]).Distinct(m_CodeComparer);
        var packs = await Task.WhenAll(codes.Select(async code =>
        {
            var contents = await Task.WhenAll(m_Sections.Select(section => readFile($"{code}/{section}.json"))).ConfigureAwait(false);
            var pack = new Dictionary<(string, string), string>();
            for (var i = 0; i < m_Sections.Length; i++)
                AddSection(pack, m_Sections[i], contents[i]);
            return (code, pack);
        })).ConfigureAwait(false);
        foreach (var (code, pack) in packs)
            catalog.m_Packs.Add(code, pack);

        var saved = catalog.Languages.FirstOrDefault(language => m_CodeComparer.Equals(language.Code, savedCode));
        catalog.Select(saved?.Code ?? catalog.Match(preferredLanguages) ?? "en");
        return catalog;
    }

    public void Select(string code)
    {
        var option = Languages.FirstOrDefault(language => m_CodeComparer.Equals(language.Code, code));
        if (option is null && !m_CodeComparer.Equals(code, "en"))
            throw new ArgumentOutOfRangeException(nameof(code));
        code = option?.Code ?? "en";
        if (Code == code)
            return;
        Code = code;
        var cultureCode = option?.MatchCodes.FirstOrDefault() ?? code;
        try { Culture = CultureInfo.GetCultureInfo(cultureCode); }
        catch (CultureNotFoundException) { Culture = CultureInfo.InvariantCulture; }
    }

    private string? Match(IEnumerable<string> preferences)
    {
        foreach (var preference in preferences)
        {
            var tag = preference;
            while (!string.IsNullOrEmpty(tag))
            {
                var match = Languages.FirstOrDefault(option => m_CodeComparer.Equals(option.Code, tag)
                    || option.MatchCodes.Contains(tag, m_CodeComparer));
                if (match is not null)
                    return match.Code;
                int separator = tag.LastIndexOf('-');
                tag = separator < 0 ? "" : tag[..separator];
            }
        }
        return null;
    }

    public string this[string systemKey] => Lookup("system", systemKey);
    public string Character(string id) => Lookup("character", id);
    public string Job(string id) => Lookup("job", id);
    public string Skill(string id) => Lookup("skill", id);
    public string Format(string systemKey, params object[] args) => Lookup("system", systemKey, args);

    private string Lookup(string section, string id, object[]? args = null)
    {
        return Read(Code) ?? (Code == "en" ? null : Read("en")) ?? (Code == "jp" ? null : Read("jp")) ?? id;

        string? Read(string code)
        {
            if (!m_Packs.TryGetValue(code, out var pack) || !pack.TryGetValue((section, id), out var value))
                return null;
            if (args is null)
                return value;
            try { return string.Format(Culture, value, args); }
            catch (FormatException) { return null; }
        }
    }

    private static IReadOnlyList<LanguageOption> ReadLanguages(string? json)
    {
        using var document = Parse(json);
        if (document?.RootElement.ValueKind != JsonValueKind.Array)
            return m_DefaultLanguages;
        var options = new List<LanguageOption>();
        var codes = new HashSet<string>(m_CodeComparer);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("code", out var id)
                || id.ValueKind != JsonValueKind.String || id.GetString() is not { Length: > 0 } code
                || !code.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                || !item.TryGetProperty("displayName", out var name) || name.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(name.GetString()) || !codes.Add(code))
                continue;
            string[] aliases = item.TryGetProperty("matchCodes", out var tags) && tags.ValueKind == JsonValueKind.Array
                ? [.. tags.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(t.GetString()))
                    .Select(t => t.GetString()!)] : [];
            options.Add(new(code, name.GetString()!, aliases));
        }
        return options.Count > 0 ? options : m_DefaultLanguages;
    }

    private static void AddSection(Dictionary<(string, string), string> pack, string section, string? json)
    {
        using var document = Parse(json);
        if (document?.RootElement.ValueKind != JsonValueKind.Object)
            return;
        foreach (var item in document.RootElement.EnumerateObject())
            if (item.Value.ValueKind == JsonValueKind.String && item.Value.GetString() is { } value && !string.IsNullOrWhiteSpace(value))
                pack[(section, item.Name)] = value;
    }

    private static JsonDocument? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return JsonDocument.Parse(json); }
        catch (JsonException) { return null; }
    }
}
