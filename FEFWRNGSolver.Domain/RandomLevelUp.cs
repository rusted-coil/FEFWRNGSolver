using System.Collections.Immutable;

namespace FEFWRNGSolver.Domain;

public readonly record struct RandomLevelUpResult(ImmutableArray<int> StatGains, uint NextGrowthSeed);

public static class RandomLevelUp
{
    private const int StatCount = 9;

    /// <summary>抽選成長を1レベル分行い、内部番号順の実際の上昇量と次の成長seedを返します。</summary>
    /// <param name="growthSeed">レベルアップ開始時の32ビットの成長seed。</param>
    /// <param name="effectiveGrowthRates">内部番号0～8の順に並べた実効成長率（％）。</param>
    /// <param name="currentStats">同じ順序でのレベルアップ前の能力値。</param>
    /// <param name="statCaps">同じ順序での各能力の上限値。上限に達した能力は抽選対象外です。</param>
    /// <param name="targetStatCount">上昇を保証する能力の種類数。人物データがない場合は0です。</param>
    public static RandomLevelUpResult Calculate(uint growthSeed, ReadOnlySpan<short> effectiveGrowthRates,
        ReadOnlySpan<int> currentStats, ReadOnlySpan<int> statCaps, byte targetStatCount)
    {
        if (effectiveGrowthRates.Length != StatCount)
            throw new ArgumentException("実効成長率を9個指定してください。", nameof(effectiveGrowthRates));
        if (currentStats.Length != StatCount)
            throw new ArgumentException("現在の能力値を9個指定してください。", nameof(currentStats));
        if (statCaps.Length != StatCount)
            throw new ArgumentException("能力上限を9個指定してください。", nameof(statCaps));

        Span<short> eligibleRates = stackalloc short[StatCount];
        for (int i = 0; i < StatCount; i++)
            eligibleRates[i] = currentStats[i] < statCaps[i] ? Math.Max((short)0, effectiveGrowthRates[i]) : (short)0;

        var roll = new GrowthRoll(eligibleRates, targetStatCount);
        int bonusMask = roll.RollBonusMask(growthSeed, out uint nextGrowthSeed);
        Span<int> gains = stackalloc int[StatCount];
        for (int i = 0; i < StatCount; i++)
        {
            int gain = eligibleRates[i] / 100 + ((bonusMask >> i) & 1);
            gains[i] = gain == 0 ? 0 : (int)Math.Min(gain, (long)statCaps[i] - currentStats[i]);
        }
        return new RandomLevelUpResult([.. gains], nextGrowthSeed);
    }
}
