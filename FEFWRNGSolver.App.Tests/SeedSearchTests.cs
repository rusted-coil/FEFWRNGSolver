using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.App.Tests;

[TestClass]
public sealed class SeedSearchTests
{
    [DataTestMethod]
    [DataRow(new short[] { 1, 2, 3, 0, 0, 0, 0, 0, 0 })]
    [DataRow(new short[] { 115, 230, 50, 75, 70, 10, 0, 25, 60 })]
    public void SplitScanMatchesForwardPredictionForConsecutiveObservations(short[] rates)
    {
        var growthRates = rates.ToImmutableArray();
        const int count = 3;
        var observations = LevelUpSimulation.Calculate(7, growthRates, count)
            .Select(step => step.StatGains.ToArray()).ToArray();
        var expected = new List<SeedSearchMatch>();
        for (uint seed = 0; seed < 512; seed++)
        {
            var steps = LevelUpSimulation.Calculate(seed, growthRates, count).ToArray();
            if (steps.Select((step, level) => step.StatGains.SequenceEqual(observations[level])).All(match => match))
                expected.Add(new SeedSearchMatch(seed, steps[^1].NextGrowthSeed));
        }

        var search = new SeedSearch(growthRates, observations);
        search.ScanRange(0, 97, default);
        search.ScanRange(97, 512, default);

        Assert.IsTrue(expected.Any(match => match.InitialSeed == 7));
        Assert.AreEqual((long)expected.Count, search.MatchCount);
        CollectionAssert.AreEqual(expected.Take(SeedSearch.MaxDisplayedMatches).ToArray(), search.Matches.ToArray());
    }

    [TestMethod]
    public void KeepsDistinctInitialSeedsThatLeadToTheSameNextSeed()
    {
        // 仕様書6章の照合値。両方の開始seedから同じ能力が伸び、保存seedも合流する。
        var search = new SeedSearch(ImmutableArray.CreateRange(Enumerable.Repeat((short)50, 9)),
            [[0, 1, 1, 1, 0, 0, 1, 1, 1]]);
        search.ScanRange(1521490162, 1521490163, default);
        search.ScanRange(1752300430, 1752300431, default);
        Assert.AreEqual(2L, search.MatchCount);
        CollectionAssert.AreEqual(new[] {
            new SeedSearchMatch(1521490162u, 524925131u),
            new SeedSearchMatch(1752300430u, 524925131u)
        }, search.Matches.ToArray());
    }

    [TestMethod]
    public void CountsAll31BitSeedsAndLimitsDisplayedCandidates()
    {
        // 2能力だけが対象のため、最低保証により必ず両方が上がる。
        var search = new SeedSearch([1, 2, 0, 0, 0, 0, 0, 0, 0], [[1, 1, 0, 0, 0, 0, 0, 0, 0]]);
        Assert.IsTrue(search.CanSkipEnumeration);
        search.ScanRange(0, 10, default);
        search.ScanRange(10, SeedSearch.CandidateCount, default);

        Assert.AreEqual(2_147_483_648L, search.MatchCount);
        Assert.AreEqual(100, search.Matches.Count);
        Assert.AreEqual(new SeedSearchMatch(0, 1900725608u), search.Matches[0]);
        Assert.AreEqual(99u, search.Matches[^1].InitialSeed);
    }

    [TestMethod]
    public void AcceptsLargestSeedAndRetriesCancelledRangeWithoutDoubleCounting()
    {
        const uint last = 0x7FFFFFFF;
        ImmutableArray<short> rates = [50, 0, 20, 200, 10, 30, 120, 0, 35];
        var observed = LevelUpSimulation.Calculate(last, rates, 1).Single();
        var search = new SeedSearch(rates, [observed.StatGains.ToArray()]);
        Assert.ThrowsException<OperationCanceledException>(() =>
            search.ScanRange(last, SeedSearch.CandidateCount, new CancellationToken(true)));
        Assert.AreEqual(0L, search.MatchCount);
        search.ScanRange(last, SeedSearch.CandidateCount, default);
        Assert.AreEqual(new SeedSearchMatch(last, observed.NextGrowthSeed), search.Matches.Single());
    }

    [DataTestMethod]
    [DataRow(new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [DataRow(new int[] { 2, 1, 0, 0, 0, 0, 0, 0, 0 })]
    public void RejectsImpossibleObservedGainsAcrossFullRange(int[] observation)
    {
        var search = new SeedSearch([25, 30, 50, 0, 0, 0, 0, 0, 0], [observation]);
        Assert.IsTrue(search.CanSkipEnumeration);
        search.ScanRange(0, SeedSearch.CandidateCount, default);
        Assert.AreEqual(0L, search.MatchCount);
        Assert.AreEqual(0, search.Matches.Count);
    }

    [TestMethod]
    public void RejectsOverlappingOrOutOfBoundsRanges()
    {
        var search = new SeedSearch(ImmutableArray.CreateRange(new short[9]), [new int[9]]);
        search.ScanRange(0, 2, default);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.ScanRange(1, 2, default));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.ScanRange(2, 1, default));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.ScanRange(2, SeedSearch.CandidateCount + 1, default));
        Assert.AreEqual(2L, search.MatchCount);
    }

    [TestMethod]
    public async Task ParallelRangesPreserveSequentialCountAndFirstCandidates()
    {
        var search = new SeedSearch([25, 30, 50, 0, 0, 0, 0, 0, 0], [[1, 0, 1, 0, 0, 0, 0, 0, 0]]);
        search.ScanRange(0, 32_768, default);
        Assert.IsTrue(search.MatchCount > SeedSearch.MaxDisplayedMatches);
        var expected = search.Matches.ToArray();
        long count = search.MatchCount;
        await using var pool = new LocalSeedSearchWorkerPool(search, action => Task.Run(action), 2);
        var upper = pool.ScanAsync(0, 16_384, 32_768, 100, default);
        var lower = pool.ScanAsync(1, 0, 16_384, 100, default);
        var ranges = await Task.WhenAll(lower, upper);

        Assert.AreEqual(count, ranges.Sum(range => range.MatchCount));
        var first = ranges.SelectMany(range => range.Matches).Take(100).ToArray();
        CollectionAssert.AreEqual(expected, first);
        Assert.AreEqual(count, search.MatchCount);
        CollectionAssert.AreEqual(expected, search.Matches.ToArray());
    }

    [TestMethod]
    public async Task RangeSampleLimitDoesNotChangeTotalAndCancelledRangesCanBeRetried()
    {
        var search = new SeedSearch(ImmutableArray.CreateRange(new short[9]), [new int[9]]);
        await using var pool = new LocalSeedSearchWorkerPool(search, null, 1);
        var total = await pool.ScanAsync(0, 0, SeedSearch.CandidateCount, 0, default);
        Assert.AreEqual(2_147_483_648L, total.MatchCount);
        Assert.AreEqual(0, total.Matches.Length);
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            pool.ScanAsync(0, 0, 10, 2, new CancellationToken(true)));
        var sampled = await pool.ScanAsync(0, 0, 10, 2, default);
        Assert.AreEqual(10L, sampled.MatchCount);
        CollectionAssert.AreEqual(new uint[] { 0, 1 }, sampled.Matches.Select(match => match.InitialSeed).ToArray());
        Assert.AreEqual(0L, search.MatchCount);
    }

    [TestMethod]
    public async Task ValidatesIndependentRangeBoundariesAndWorkerIndex()
    {
        var search = new SeedSearch(ImmutableArray.CreateRange(new short[9]), [new int[9]]);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.EvaluateRange(-1, 10, 0, default));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.EvaluateRange(10, 9, 0, default));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.EvaluateRange(0, SeedSearch.CandidateCount + 1, 0, default));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => search.EvaluateRange(0, 10, -1, default));
        var empty = search.EvaluateRange(SeedSearch.CandidateCount, SeedSearch.CandidateCount, 1, default);
        Assert.AreEqual(0L, empty.MatchCount);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LocalSeedSearchWorkerPool(search, null, 0));
        await using var pool = new LocalSeedSearchWorkerPool(search, null, 2);
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => pool.ScanAsync(2, 0, 1, 1, default));
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => pool.ScanAsync(-1, 0, 1, 1, default));
    }
}
