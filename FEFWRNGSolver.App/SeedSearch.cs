using System.Collections.Immutable;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.App;

public readonly record struct SeedSearchMatch(uint InitialSeed, uint NextSeed);
public sealed record SeedSearchRangeResult(long MatchCount, SeedSearchMatch[] Matches);

/// <summary>最初の観測直前の31ビットseedを調べ、観測直後のseedとともに返します。</summary>
public sealed class SeedSearch
{
    public const long CandidateCount = 1L << 31;
    public const int MaxDisplayedMatches = 100;
    private readonly ObservedGrowthSequence m_Observations;
    private readonly List<SeedSearchMatch> m_Matches = new(MaxDisplayedMatches);
    private long m_ScannedUntil;

    public SeedSearch(ImmutableArray<short> effectiveRates, IReadOnlyList<int[]> observations, byte targetStatCount = 0)
    {
        m_Observations = new ObservedGrowthSequence(effectiveRates.AsSpan(), targetStatCount, observations);
        Matches = m_Matches.AsReadOnly();
    }

    public long MatchCount { get; private set; }
    public IReadOnlyList<SeedSearchMatch> Matches { get; }
    public bool CanSkipEnumeration => !m_Observations.CanMatch || m_Observations.MatchesEverySeed;

    /// <summary>重複しない範囲を小さい順に走査します。中断した範囲の結果は加算しません。</summary>
    public void ScanRange(long startInclusive, long endExclusive, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startInclusive, m_ScannedUntil);
        var result = EvaluateRange(startInclusive, endExclusive, MaxDisplayedMatches - m_Matches.Count, cancellationToken);
        m_Matches.AddRange(result.Matches);
        MatchCount += result.MatchCount;
        m_ScannedUntil = endExclusive;
    }

    /// <summary>区間内の総件数と開始seed昇順の先頭候補を返します。複数区間を並列に照合できます。</summary>
    public SeedSearchRangeResult EvaluateRange(long startInclusive, long endExclusive, int sampleLimit,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startInclusive);
        ArgumentOutOfRangeException.ThrowIfLessThan(endExclusive, startInclusive);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endExclusive, CandidateCount);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleLimit);
        cancellationToken.ThrowIfCancellationRequested();
        long matches = 0;
        List<SeedSearchMatch>? samples = null;
        if (m_Observations.CanMatch)
        {
            bool everySeed = m_Observations.MatchesEverySeed;
            long scanEnd = everySeed ? Math.Min(startInclusive + sampleLimit, endExclusive) : endExclusive;
            for (long seed = startInclusive; seed < scanEnd; seed++)
            {
                if ((seed & 1023) == 0)
                    cancellationToken.ThrowIfCancellationRequested();
                if (m_Observations.TryMatch((uint)seed, out uint nextSeed))
                {
                    if (matches < sampleLimit)
                        (samples ??= []).Add(new SeedSearchMatch((uint)seed, nextSeed));
                    matches++;
                }
            }
            if (everySeed)
                matches = endExclusive - startInclusive;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new SeedSearchRangeResult(matches, samples?.ToArray() ?? []);
    }
}
