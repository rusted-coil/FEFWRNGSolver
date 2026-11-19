namespace FEFWRNGSolver.Domain;

/// <summary>同じ実効率・最低保証で、能力上限に達しない連続した抽選成長の観測。</summary>
public sealed class ObservedGrowthSequence
{
    private readonly GrowthRoll m_Roll;
    private readonly int[] m_BonusMasks;

    public ObservedGrowthSequence(ReadOnlySpan<short> rates, byte targetStatCount, IReadOnlyList<int[]> observations)
    {
        if (rates.Length != 9 || rates.ContainsAnyExceptInRange((short)0, short.MaxValue))
            throw new ArgumentException("実効成長率を0以上の整数で9個指定してください。", nameof(rates));
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
            throw new ArgumentException("観測を1件以上指定してください。", nameof(observations));

        m_Roll = new GrowthRoll(rates, targetStatCount);
        m_BonusMasks = new int[observations.Count];
        CanMatch = true;
        for (int level = 0; level < observations.Count; level++)
        {
            var gains = observations[level];
            if (gains is not { Length: 9 })
                throw new ArgumentException("各観測には内部順で9個の上昇量を指定してください。", nameof(observations));
            int mask = 0;
            for (int i = 0; i < rates.Length; i++)
            {
                int guaranteed = rates[i] / 100;
                if (gains[i] == guaranteed)
                    continue;
                if (rates[i] % 100 != 0 && gains[i] == guaranteed + 1)
                    mask |= 1 << i;
                else
                    CanMatch = false;
            }
            m_BonusMasks[level] = mask;
            if (!m_Roll.MeetsGuarantee(mask))
                CanMatch = false;
        }
    }

    /// <summary>確定上昇量と最低保証に矛盾しない観測です。</summary>
    public bool CanMatch { get; }
    public bool MatchesEverySeed => CanMatch && m_Roll.IsDeterministic;

    public bool TryMatch(uint seed, out uint nextSeed)
    {
        nextSeed = seed;
        if (!CanMatch)
            return false;
        foreach (int expected in m_BonusMasks)
        {
            if (m_Roll.IsDeterministic)
                nextSeed = new GrowthRandom(nextSeed).Next31();
            else if (m_Roll.RollBonusMask(nextSeed, out nextSeed) != expected)
                return false;
        }
        return true;
    }
}
