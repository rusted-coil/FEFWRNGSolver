using System.Collections.Immutable;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.App;

public readonly record struct LevelUpStep(uint GrowthSeed, ImmutableArray<int> StatGains, uint NextGrowthSeed);

public static class LevelUpSimulation
{
    /// <summary>選択したユニット、兵種、動物、スキル補正から実効成長率を求めます。</summary>
    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, GrowthRateMasterData masterData,
        UnitGrowthSetup setup, int levelUpCount)
    {
        ArgumentNullException.ThrowIfNull(masterData);
        return Calculate(growthSeed, masterData.Calculate(setup), levelUpCount);
    }

    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, ImmutableArray<short> baseRates,
        GrowthSkillBonus skillBonus, int levelUpCount) =>
        Calculate(growthSeed, EffectiveGrowthRates.Calculate(baseRates.AsSpan(), skillBonus), levelUpCount);

    /// <summary>現在値0、全能力の上限99、最低保証2種類の条件で抽選成長を繰り返します。</summary>
    public static IEnumerable<LevelUpStep> Calculate(uint growthSeed, ImmutableArray<short> effectiveGrowthRates,
        int levelUpCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(levelUpCount, 1);
        var currentStats = new int[9];
        var statCaps = Enumerable.Repeat(99, currentStats.Length).ToArray();
        for (int level = 0; level < levelUpCount; level++)
        {
            var result = RandomLevelUp.Calculate(growthSeed, effectiveGrowthRates.AsSpan(), currentStats, statCaps, 2);
            yield return new LevelUpStep(growthSeed, result.StatGains, result.NextGrowthSeed);
            growthSeed = result.NextGrowthSeed;
            for (int i = 0; i < currentStats.Length; i++)
                currentStats[i] += result.StatGains[i];
        }
    }
}
