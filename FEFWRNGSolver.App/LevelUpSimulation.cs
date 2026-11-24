using System.Collections.Immutable;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.App;

public readonly record struct LevelUpStep(uint GrowthSeed, ImmutableArray<int> StatGains, uint NextGrowthSeed)
{
    public int MinimumExamPassRate => CertificationExam.GetMinimumPassRate(GrowthSeed);
}

public static class LevelUpSimulation
{
    /// <summary>選択したユニット、兵種、動物、スキル補正から実効成長率を求めます。</summary>
    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, GrowthRateMasterData masterData,
        UnitGrowthSetup setup, int levelUpCount)
    {
        ArgumentNullException.ThrowIfNull(masterData);
        var target = masterData.Units[setup.UnitId].GetGuaranteedStatCount(setup.IsPartThree);
        return Calculate(growthSeed, masterData.Calculate(setup), levelUpCount, [], target);
    }

    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, ImmutableArray<short> baseRates,
        GrowthSkillBonus skillBonus, int levelUpCount) =>
        Calculate(growthSeed, EffectiveGrowthRates.Calculate(baseRates.AsSpan(), skillBonus), levelUpCount);

    /// <summary>現在値0、全能力の上限99、最低保証なしで抽選成長を繰り返します。</summary>
    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, ImmutableArray<short> effectiveGrowthRates,
        int levelUpCount) => Calculate(growthSeed, effectiveGrowthRates, levelUpCount, []);

    /// <summary>指定した内部番号の能力は最初から上限に達した状態で、抽選成長を繰り返します。</summary>
    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, ImmutableArray<short> effectiveGrowthRates,
        int levelUpCount, ImmutableArray<int> cappedStatIndices, byte targetStatCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(levelUpCount, 1);
        return Simulate(growthSeed, [("", effectiveGrowthRates)], levelUpCount - 1, cappedStatIndices, targetStatCount,
            carryStatGains: true).Select(result => result.Step);
    }

    /// <summary>能力値を固定し、乱数を0～maxAdvances回消費した各時点での次のレベルアップを予測します。</summary>
    public static IEnumerable<LevelUpStep> PredictAtAdvances(uint growthSeed, ImmutableArray<short> effectiveGrowthRates,
        int maxAdvances, ImmutableArray<int> cappedStatIndices, byte targetStatCount) =>
        PredictClassesAtAdvances(growthSeed, [("", effectiveGrowthRates)], maxAdvances, cappedStatIndices,
            targetStatCount).Select(result => result.Step);

    /// <summary>各消費数の同じ成長seedから候補兵種をそれぞれ予測し、消費数・候補順で返します。</summary>
    public static IEnumerable<(int Advances, string ClassId, LevelUpStep Step)> PredictClassesAtAdvances(uint growthSeed,
        IReadOnlyList<(string ClassId, ImmutableArray<short> Rates)> classes, int maxAdvances,
        ImmutableArray<int> cappedStatIndices, byte targetStatCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxAdvances);
        return Simulate(growthSeed, classes, maxAdvances, cappedStatIndices, targetStatCount, carryStatGains: false);
    }

    private static IEnumerable<(int Advances, string ClassId, LevelUpStep Step)> Simulate(uint growthSeed,
        IReadOnlyList<(string ClassId, ImmutableArray<short> Rates)> classes,
        int maxAdvances, ImmutableArray<int> cappedStatIndices, byte targetStatCount, bool carryStatGains)
    {
        if (classes.Count == 0)
            yield break;
        var currentStats = new int[9];
        var statCaps = Enumerable.Repeat(99, currentStats.Length).ToArray();
        foreach (int index in cappedStatIndices)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(cappedStatIndices));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, currentStats.Length, nameof(cappedStatIndices));
            currentStats[index] = statCaps[index];
        }
        for (int remaining = maxAdvances; remaining >= 0; remaining--)
        {
            uint nextSeed = growthSeed;
            foreach (var (classId, rates) in classes)
            {
                var result = RandomLevelUp.Calculate(growthSeed, rates.AsSpan(), currentStats, statCaps, targetStatCount);
                yield return (maxAdvances - remaining, classId, new(growthSeed, result.StatGains, result.NextGrowthSeed));
                nextSeed = result.NextGrowthSeed;
                if (carryStatGains)
                {
                    for (int i = 0; i < currentStats.Length; i++)
                        currentStats[i] += result.StatGains[i];
                }
            }
            growthSeed = nextSeed;
        }
    }
}
