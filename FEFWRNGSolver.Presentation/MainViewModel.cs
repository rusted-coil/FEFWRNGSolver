using System.Collections.Immutable;
using System.Globalization;
using FEFWRNGSolver.App;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed class MainViewModel
{
    private readonly IUnitSettingsStore m_Store;
    private GrowthSkillBonus m_SelectedSkillBonus;

    public string Title => "万紫千紅 乱数調整ツール";
    public string GrowthSeed { get; set; } = "";
    public string LevelUpCount { get; set; } = "1";
    public GrowthSetupViewModel Setup { get; } = new();
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
            if (m_SelectedSkillBonus == value)
                return;
            m_SelectedSkillBonus = value;
            OnSettingsChanged();
        }
    }
    public static IReadOnlyList<SkillBonusOption> SkillBonusOptions { get; } =
    [
        new(GrowthSkillBonus.None, "スキル補正無し"),
        new(GrowthSkillBonus.CharioteersPath35, "戦車兵の道（Lv35）"),
        new(GrowthSkillBonus.CharioteersPath45, "戦車兵の道（Lv45）"),
        new(GrowthSkillBonus.ElephantRidersPath45, "戦象兵の道（Lv45）")
    ];
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<LevelUpRow> Results { get; private set; } = [];
    public event Action? UnitSelected;

    // 表示順で並べ、内部番号には仕様書にある本作専用セーブエディタの保存配列順を使う。
    public IReadOnlyList<GrowthRateInput> GrowthRates { get; } =
    [
        new("HP", 0), new("力", 1), new("魔力", 2), new("速さ", 4), new("技", 3),
        new("守備", 6), new("魔防", 7), new("幸運", 5), new("魅力", 8)
    ];

    public MainViewModel() : this(new MemoryUnitSettingsStore()) { }

    public MainViewModel(IUnitSettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        m_Store = store;
        Setup.UnitChanged += RestoreUnit;
        Setup.Changed += OnSettingsChanged;
    }

    private void RestoreUnit()
    {
        var settings = Setup.IsManual ? null : m_Store.Load(Setup.UnitId);
        Setup.RestoreSelection(settings);
        if (!Setup.IsManual)
            m_SelectedSkillBonus = settings is not null && Enum.IsDefined(settings.SkillBonus)
                ? settings.SkillBonus : GrowthSkillBonus.None;
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
        m_Store.Save(Setup.UnitId, new(Setup.ClassId, Setup.AnimalId, Setup.Affection, SelectedSkillBonus, seed));
    }

    public void AdoptGrowthSeed(uint seed)
    {
        GrowthSeed = seed.ToString("X8");
        SaveSettings(seed);
        ClearPrediction();
    }

    private void ClearPrediction()
    {
        Results = [];
        ErrorMessage = null;
    }

    private void RefreshGrowthRates()
    {
        var selectedRates = SelectedGrowthRates;
        var skill = EffectiveGrowthRates.GetSkillModifiers(SelectedSkillBonus);
        for (int i = 0; i < GrowthRates.Count; i++)
        {
            var input = GrowthRates[i];
            input.ApplySetup(Setup.IsManual ? null : selectedRates[i], skill[input.InternalIndex]);
        }
    }

    public bool Calculate()
    {
        if (GrowthSeed is not { Length: 8 } || !uint.TryParse(GrowthSeed, NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture, out uint seed))
            return Fail("成長seedを8桁の16進数で入力してください（例：1234ABCD）。");

        SaveSettings(seed);
        if (!int.TryParse(LevelUpCount, NumberStyles.None, CultureInfo.InvariantCulture, out int count)
            || count is < 1 or > 10_000)
            return Fail("レベルアップ回数は1～10000の整数で入力してください。");

        if (!TryGetEffectiveRates(out var rates, out string error))
            return Fail(error);

        var steps = LevelUpSimulation.Calculate(seed, rates, count);
        var rows = new List<LevelUpRow>(count);
        foreach (var step in steps)
        {
            var gains = GrowthRates.Select(input => step.StatGains[input.InternalIndex]).ToImmutableArray();
            rows.Add(new LevelUpRow(rows.Count + 1, step.GrowthSeed.ToString("X8"), gains, step.NextGrowthSeed.ToString("X8")));
        }
        Results = rows.AsReadOnly();
        ErrorMessage = null;
        return true;
    }

    internal bool TryGetEffectiveRates(out ImmutableArray<short> effectiveRates, out string error)
    {
        effectiveRates = default;
        error = "";
        Span<short> rates = stackalloc short[GrowthRates.Count];
        foreach (var input in GrowthRates)
        {
            if (!short.TryParse(input.Rate, NumberStyles.None, CultureInfo.InvariantCulture, out rates[input.InternalIndex]))
            {
                error = $"{input.Name}の実効成長率は0～32767の整数で入力してください。";
                return false;
            }
        }
        effectiveRates = [.. rates];
        return true;
    }

    private bool Fail(string message)
    {
        ErrorMessage = message;
        return false;
    }
}

public sealed record SkillBonusOption(GrowthSkillBonus Value, string Name);

public sealed class GrowthRateInput
{
    private string? m_UnitRate;
    private string m_ManualRate = "0";
    private short m_SkillBonusAtInput;
    private short m_SkillBonus;

    internal GrowthRateInput(string name, int internalIndex) => (Name, InternalIndex) = (name, internalIndex);

    public string Name { get; }
    public string Rate
    {
        get
        {
            if (m_UnitRate is not null)
                return m_UnitRate;
            // 入力時の補正との差を使い、スキルを往復しても丸め前の値を復元する。
            if (m_SkillBonus != m_SkillBonusAtInput &&
                short.TryParse(m_ManualRate, NumberStyles.None, CultureInfo.InvariantCulture, out short rate))
                return Math.Max(0, rate + m_SkillBonus - m_SkillBonusAtInput).ToString(CultureInfo.InvariantCulture);
            return m_ManualRate;
        }
        set
        {
            if (m_UnitRate is not null)
                m_UnitRate = value;
            else
            {
                m_ManualRate = value;
                m_SkillBonusAtInput = m_SkillBonus;
            }
        }
    }
    internal int InternalIndex { get; }

    internal void ApplySetup(short? unitRate, short skillBonus)
    {
        m_UnitRate = unitRate?.ToString(CultureInfo.InvariantCulture);
        m_SkillBonus = skillBonus;
    }
}

public sealed record LevelUpRow(int Number, string GrowthSeed, ImmutableArray<int> StatGains, string NextGrowthSeed);
