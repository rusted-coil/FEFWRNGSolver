using System.Collections.Immutable;
using System.Globalization;
using FEFWRNGSolver.App;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed class MainViewModel
{
    private readonly IUnitSettingsStore m_Store;
    private readonly LocalizationCatalog m_Text;
    private ValidationError? m_Error;
    private GrowthSkillBonus m_SelectedSkillBonus;
    private bool m_IsPartThree;
    private string m_ManualGuaranteedStatCount = "2";

    public string Title => m_Text["SYS_APP_TITLE"];
    public string GrowthSeed { get; set; } = "";
    public string MaxAdvances { get; set; } = RngAdvanceInput.DefaultMaximum;
    public int MinGrowthCount { get; set; }
    public int MaxGrowthCount { get; set; } = 9;
    public IReadOnlyList<GrowthConditionOption> GrowthConditionOptions =>
    [
        new(StatGrowthCondition.Capped, m_Text["SYS_FILTER_CAPPED"]),
        new(StatGrowthCondition.MustGrow, m_Text["SYS_FILTER_MUST_GROW"]),
        new(StatGrowthCondition.Any, m_Text["SYS_FILTER_ANY"]),
        new(StatGrowthCondition.MustNotGrow, m_Text["SYS_FILTER_MUST_NOT_GROW"])
    ];
    public GrowthSetupViewModel Setup { get; }
    public bool CanSelectPartThree => Setup.SelectedUnit?.HasPrePartThreeGuarantee == true;
    public string GuaranteedStatCount
    {
        get => Setup.SelectedUnit?.GetGuaranteedStatCount(IsPartThree).ToString(CultureInfo.InvariantCulture)
            ?? m_ManualGuaranteedStatCount;
        set
        {
            if (Setup.IsManual)
                m_ManualGuaranteedStatCount = value;
        }
    }
    public bool IsPartThree
    {
        get => m_IsPartThree;
        set
        {
            if (!CanSelectPartThree || m_IsPartThree == value)
                return;
            m_IsPartThree = value;
            SaveSettings();
        }
    }
    public ImmutableArray<short> SelectedGrowthRates
    {
        get
        {
            if (Setup.IsManual)
                return [];
            var rates = Setup.GetEffectiveRates(SelectedSkillBonus);
            return [.. GrowthRates.Select(stat => rates[stat.InternalIndex])];
        }
    }
    public GrowthSkillBonus SelectedSkillBonus
    {
        get => m_SelectedSkillBonus;
        set
        {
            if (m_SelectedSkillBonus == value || !IsSkillAvailable(value))
                return;
            m_SelectedSkillBonus = value;
            OnSettingsChanged();
        }
    }
    public IReadOnlyList<SkillBonusOption> SkillOptions =>
        [.. GetSkillOptions(m_Text).Where(option => IsSkillAvailable(option.Value))];
    public static IReadOnlyList<SkillBonusOption> SkillBonusOptions => GetSkillOptions(LocalizationCatalog.Default);

    private bool IsSkillAvailable(GrowthSkillBonus skill) => skill switch
    {
        GrowthSkillBonus.None => true,
        GrowthSkillBonus.CharioteersPath35 or GrowthSkillBonus.CharioteersPath45 => Setup.ClassId == "JID_戦車兵",
        GrowthSkillBonus.ElephantRidersPath45 => Setup.ClassId == "JID_戦象兵",
        _ => false
    };

    private static SkillBonusOption[] GetSkillOptions(LocalizationCatalog text) =>
    [
        new(GrowthSkillBonus.None, text["SYS_NO_SKILL_BONUS"]),
        new(GrowthSkillBonus.CharioteersPath35, text.Format("SYS_SKILL_LEVEL_FORMAT", text.Skill("SID_戦車兵の道"), 35)),
        new(GrowthSkillBonus.CharioteersPath45, text.Format("SYS_SKILL_LEVEL_FORMAT", text.Skill("SID_戦車兵の道"), 45)),
        new(GrowthSkillBonus.ElephantRidersPath45, text.Format("SYS_SKILL_LEVEL_FORMAT", text.Skill("SID_戦象兵の道"), 45))
    ];
    public string? ErrorMessage => m_Error?.Translate(m_Text);
    public bool HasCalculated { get; private set; }
    public IReadOnlyList<LevelUpRow> Results { get; private set; } = [];
    public event Action? UnitSelected;

    public IReadOnlyList<GrowthRateInput> GrowthRates { get; }

    public MainViewModel() : this(new MemoryUnitSettingsStore()) { }

    public MainViewModel(IUnitSettingsStore store) : this(store, LocalizationCatalog.Default) { }

    public MainViewModel(IUnitSettingsStore store, LocalizationCatalog text, GrowthRateMasterData? masterData = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(text);
        m_Store = store;
        m_Text = text;
        // 表示順で並べ、内部番号には仕様書にある本作専用セーブエディタの保存配列順を使う。
        GrowthRates =
        [
            new(text, "SYS_HP_LABEL", 0), new(text, "SYS_STRENGTH_LABEL", 1), new(text, "SYS_MAGIC_LABEL", 2),
            new(text, "SYS_SPEED_LABEL", 4), new(text, "SYS_DEXTERITY_LABEL", 3), new(text, "SYS_DEFENSE_LABEL", 6),
            new(text, "SYS_RESISTANCE_LABEL", 7), new(text, "SYS_LUCK_LABEL", 5), new(text, "SYS_CHARM_LABEL", 8)
        ];
        Setup = new(text, masterData ?? GrowthRateMasterData.Default);
        Setup.UnitChanged += RestoreUnit;
        Setup.Changed += OnSettingsChanged;
    }

    private void RestoreUnit()
    {
        var settings = Setup.IsManual ? null : m_Store.Load(Setup.UnitId);
        Setup.RestoreSelection(settings);
        m_IsPartThree = CanSelectPartThree && settings?.IsPartThree == true;
        m_SelectedSkillBonus = settings?.SkillBonus ?? GrowthSkillBonus.None;
        GrowthSeed = settings?.GrowthSeed?.ToString("X8") ?? "";
        RefreshGrowthRates();
        ClearPrediction();
        UnitSelected?.Invoke();
    }

    private void OnSettingsChanged()
    {
        RefreshGrowthRates();
        SaveSettings();
    }

    private void SaveSettings(uint? seed = null)
    {
        if (Setup.IsManual)
            return;
        // seed欄には入力途中の値もあるので、設定だけの変更では保存済みのseedを引き継ぐ。
        seed ??= m_Store.Load(Setup.UnitId)?.GrowthSeed;
        m_Store.Save(Setup.UnitId, new(Setup.ClassId, Setup.AnimalId, Setup.Affection, SelectedSkillBonus, seed, IsPartThree));
    }

    public void AdoptGrowthSeed(uint seed)
    {
        GrowthSeed = seed.ToString("X8");
        SaveSettings(seed);
        ClearPrediction();
    }

    private void ClearPrediction()
    {
        HasCalculated = false;
        Results = [];
        m_Error = null;
    }

    private void RefreshGrowthRates()
    {
        if (!IsSkillAvailable(m_SelectedSkillBonus))
            m_SelectedSkillBonus = GrowthSkillBonus.None;
        var selectedRates = SelectedGrowthRates;
        for (int i = 0; i < GrowthRates.Count; i++)
            GrowthRates[i].ApplySetup(Setup.IsManual ? null : selectedRates[i]);
    }

    public bool Calculate()
    {
        if (GrowthSeed is not { Length: 8 } || !uint.TryParse(GrowthSeed, NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture, out uint seed))
            return Fail(new("SYS_INVALID_GROWTH_SEED"));

        SaveSettings(seed);
        if (!RngAdvanceInput.TryParseMaximum(MaxAdvances, out int maxAdvances))
            return Fail(new("SYS_INVALID_MAX_RNG_ADVANCES"));

        if (MinGrowthCount < 0 || MaxGrowthCount > GrowthRates.Count || MinGrowthCount > MaxGrowthCount)
            return Fail(new("SYS_INVALID_GROWTH_COUNT"));

        if (!TryGetGrowthSettings(out var rates, out var targetStatCount, out var error))
            return Fail(error);

        var cappedStats = GrowthRates.Where(input => input.Condition == StatGrowthCondition.Capped)
            .Select(input => input.InternalIndex).ToImmutableArray();
        var steps = LevelUpSimulation.PredictAtAdvances(seed, rates, maxAdvances, cappedStats, targetStatCount);
        var rows = new List<LevelUpRow>(maxAdvances + 1);
        foreach (var (advances, step) in steps.Index())
        {
            if (!MatchesGrowthConditions(step.StatGains))
                continue;
            var gains = GrowthRates.Select(input => step.StatGains[input.InternalIndex]).ToImmutableArray();
            rows.Add(new LevelUpRow(advances, step.GrowthSeed.ToString("X8"), gains, step.NextGrowthSeed.ToString("X8")));
        }
        Results = rows.AsReadOnly();
        HasCalculated = true;
        m_Error = null;
        return true;
    }

    private bool MatchesGrowthConditions(ImmutableArray<int> gains)
    {
        int growthCount = 0;
        foreach (var input in GrowthRates)
        {
            bool grew = gains[input.InternalIndex] > 0;
            if ((input.Condition == StatGrowthCondition.MustGrow && !grew) ||
                (input.Condition == StatGrowthCondition.MustNotGrow && grew))
                return false;
            if (grew)
                growthCount++;
        }
        return growthCount >= MinGrowthCount && growthCount <= MaxGrowthCount;
    }

    internal bool TryGetGrowthSettings(out ImmutableArray<short> rates, out byte targetStatCount, out ValidationError error)
    {
        if (byte.TryParse(GuaranteedStatCount, NumberStyles.None, CultureInfo.InvariantCulture, out targetStatCount)
            && targetStatCount <= GrowthRates.Count)
            return TryGetEffectiveRates(out rates, out error);

        rates = default;
        error = new("SYS_INVALID_GUARANTEED_STAT_COUNT");
        return false;
    }

    internal bool TryGetEffectiveRates(out ImmutableArray<short> effectiveRates, out ValidationError error)
    {
        effectiveRates = default;
        error = default;
        Span<short> rates = stackalloc short[GrowthRates.Count];
        foreach (var input in GrowthRates)
        {
            if (!short.TryParse(input.Rate, NumberStyles.None, CultureInfo.InvariantCulture, out rates[input.InternalIndex]))
            {
                error = new("SYS_INVALID_GROWTH_RATE", input.NameKey);
                return false;
            }
        }
        effectiveRates = [.. rates];
        return true;
    }

    private bool Fail(ValidationError error)
    {
        m_Error = error;
        return false;
    }
}

public sealed record SkillBonusOption(GrowthSkillBonus Value, string Name);

public enum StatGrowthCondition { Capped, MustGrow, Any, MustNotGrow }

public sealed record GrowthConditionOption(StatGrowthCondition Value, string Name);

internal readonly record struct ValidationError(string Key, string? StatNameKey = null)
{
    public string Translate(LocalizationCatalog text) =>
        StatNameKey is null ? text[Key] : text.Format(Key, text[StatNameKey]);
}

public sealed class GrowthRateInput
{
    private readonly LocalizationCatalog m_Text;
    private string? m_UnitRate;
    private string m_ManualRate = "0";

    internal GrowthRateInput(LocalizationCatalog text, string nameKey, int internalIndex) =>
        (m_Text, NameKey, InternalIndex) = (text, nameKey, internalIndex);

    internal string NameKey { get; }
    public string Name => m_Text[NameKey];
    public StatGrowthCondition Condition { get; set; } = StatGrowthCondition.Any;
    public string Rate
    {
        get => m_UnitRate ?? m_ManualRate;
        set
        {
            if (m_UnitRate is not null)
                m_UnitRate = value;
            else
                m_ManualRate = value;
        }
    }
    internal int InternalIndex { get; }

    internal void ApplySetup(short? unitRate) =>
        m_UnitRate = unitRate?.ToString(CultureInfo.InvariantCulture);
}

public sealed record LevelUpRow(int Advances, string GrowthSeed, ImmutableArray<int> StatGains, string NextGrowthSeed);
