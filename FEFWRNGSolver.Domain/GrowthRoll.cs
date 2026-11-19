using System.Numerics;
using System.Runtime.CompilerServices;

namespace FEFWRNGSolver.Domain;

/// <summary>上限到達済みの能力を除いた、1レベル分の抽選条件。</summary>
internal struct GrowthRoll
{
    private readonly record struct Draw(int Bit, uint Threshold, int Weight);

    [InlineArray(9)]
    private struct DrawBuffer
    {
        private Draw m_First;
    }

    private DrawBuffer m_Draws;
    private readonly int m_DrawCount;
    private readonly int m_GuaranteedMask;
    private readonly int m_Target;

    public GrowthRoll(ReadOnlySpan<short> rates, byte target)
    {
        int eligible = 0;
        int fractionalMask = 0;
        for (int i = 0; i < rates.Length; i++)
        {
            int rate = rates[i];
            if (rate <= 0)
                continue;
            eligible++;
            int bit = 1 << i;
            if (rate >= 100)
                m_GuaranteedMask |= bit;
            int remainder = rate % 100;
            if (remainder > 0)
            {
                fractionalMask |= bit;
                m_Draws[m_DrawCount++] = new Draw(bit, (uint)(remainder * 10_000), rate * 100);
            }
        }
        m_Target = Math.Min((int)target, eligible);
        IsDeterministic = m_DrawCount == 0 ||
            (m_Target == eligible && (m_GuaranteedMask & fractionalMask) == 0);
    }

    public readonly bool IsDeterministic { get; }

    public readonly bool MeetsGuarantee(int bonusMask) =>
        BitOperations.PopCount((uint)(m_GuaranteedMask | bonusMask)) >= m_Target;

    // 確定分（成長率 / 100）にさらに1が加わった能力をビットで返す。
    public readonly int RollBonusMask(uint seed, out uint nextSeed)
    {
        var random = new GrowthRandom(seed);
        var seedUpdate = random;
        nextSeed = seedUpdate.Next31();
        int bonusMask = 0;
        int grown = 0;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            bonusMask = 0;
            for (int i = 0; i < m_DrawCount; i++)
            {
                ref readonly var draw = ref m_Draws[i];
                if (random.Next31() % 1_000_000 < draw.Threshold)
                    bonusMask |= draw.Bit;
            }
            grown = BitOperations.PopCount((uint)(m_GuaranteedMask | bonusMask));
            if (grown >= Math.Max(1, m_Target))
                break;
        }
        while (grown < m_Target)
        {
            int remaining = ~(m_GuaranteedMask | bonusMask);
            int totalWeight = 0;
            for (int i = 0; i < m_DrawCount; i++)
                if ((remaining & m_Draws[i].Bit) != 0)
                    totalWeight += m_Draws[i].Weight;

            int ticket = (int)random.Next31() % totalWeight;
            for (int i = 0; i < m_DrawCount; i++)
            {
                ref readonly var draw = ref m_Draws[i];
                if ((remaining & draw.Bit) == 0)
                    continue;
                if (ticket < draw.Weight)
                {
                    bonusMask |= draw.Bit;
                    grown++;
                    break;
                }
                ticket -= draw.Weight;
            }
        }
        return bonusMask;
    }
}
