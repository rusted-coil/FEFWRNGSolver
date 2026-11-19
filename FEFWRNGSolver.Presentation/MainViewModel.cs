using System.Collections.Immutable;
using System.Globalization;
using FEFWRNGSolver.App;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed class MainViewModel
{
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
    public GrowthSkillBonus SelectedSkillBonus { get; set; } = GrowthSkillBonus.None;
    public static IReadOnlyList<SkillBonusOption> SkillBonusOptions { get; } =
    [
        new(GrowthSkillBonus.None, "スキル補正無し"),
        new(GrowthSkillBonus.CharioteersPath35, "戦車兵の道（Lv35）"),
        new(GrowthSkillBonus.CharioteersPath45, "戦車兵の道（Lv45）"),
        new(GrowthSkillBonus.ElephantRidersPath45, "戦象兵の道（Lv45）")
    ];
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<LevelUpRow> Results { get; private set; } = [];

    // 表示順で並べ、内部番号には仕様書にある本作専用セーブエディタの保存配列順を使う。
    public IReadOnlyList<GrowthRateInput> GrowthRates { get; } =
    [
        new("HP", 0), new("力", 1), new("魔力", 2), new("速さ", 4), new("技", 3),
        new("守備", 6), new("魔防", 7), new("幸運", 5), new("魅力", 8)
    ];

    public bool Calculate()
    {
        if (GrowthSeed is not { Length: 8 } || !uint.TryParse(GrowthSeed, NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture, out uint seed))
            return Fail("成長seedを8桁の16進数で入力してください（例：1234ABCD）。");

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
        try
        {
            if (!Setup.IsManual)
            {
                effectiveRates = Setup.GetEffectiveRates(SelectedSkillBonus);
                return true;
            }

            Span<short> rates = stackalloc short[GrowthRates.Count];
            bool hasSkill = SelectedSkillBonus != GrowthSkillBonus.None;
            var numberStyle = hasSkill ? NumberStyles.AllowLeadingSign : NumberStyles.None;
            foreach (var input in GrowthRates)
            {
                if (!short.TryParse(input.Rate, numberStyle, CultureInfo.InvariantCulture, out rates[input.InternalIndex]))
                {
                    error = $"{input.Name}の成長率は{(hasSkill ? short.MinValue : 0)}～32767の整数で入力してください。";
                    return false;
                }
            }
            effectiveRates = EffectiveGrowthRates.Calculate(rates, SelectedSkillBonus);
            return true;
        }
        catch (OverflowException)
        {
            error = "スキル補正後の成長率は各能力32767以下にしてください。";
            return false;
        }
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
    internal GrowthRateInput(string name, int internalIndex) => (Name, InternalIndex) = (name, internalIndex);

    public string Name { get; }
    public string Rate { get; set; } = "0";
    internal int InternalIndex { get; }
}

public sealed record LevelUpRow(int Number, string GrowthSeed, ImmutableArray<int> StatGains, string NextGrowthSeed);
