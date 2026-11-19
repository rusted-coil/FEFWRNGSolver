using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Domain.Tests;

[TestClass]
public sealed class RandomLevelUpTests
{
    [DataTestMethod]
    [DataRow(0u, new short[] { 30, 40, 50 }, new int[0], 2, "110000000", 1900725608u)]
    [DataRow(0u, new short[] { 1, 2 }, new int[0], 1, "010000000", 1900725608u)]
    [DataRow(123456789u, new short[] { 1, 2, 3 }, new int[0], 2, "011000000", 286067900u)]
    [DataRow(123456789u, new short[] { 1, 2, 3 }, new int[0], 0, "000000000", 286067900u)]
    [DataRow(123456789u, new short[] { 0, 100, 200, 230, 20 }, new[] { 19, 20, 21, 14, 15 }, 1, "012100000", 286067900u)]
    [DataRow(123456789u, new short[] { 50, 20, 100 }, new[] { 10 }, 1, "011000000", 286067900u)]
    [DataRow(0u, new short[] { 100, 20 }, new[] { 10, 10 }, 255, "000000000", 1900725608u)]
    [DataRow(0u, new short[] { 1, 2 }, new int[0], 255, "110000000", 1900725608u)]
    [DataRow(1623u, new short[] { 36, 100 }, new int[0], 1, "010000000", 2064360000u)]
    [DataRow(0xFFFFFFFFu, new short[] { 50, 50, 50, 50, 50, 50, 50, 50, 50 }, new int[0], 1, "001011001", 37784296u)]
    public void CalculatesStatGainsAndNextGrowthSeed(uint growthSeed, short[] rates, int[] capOverrides,
        int targetStatCount, string expectedGains, uint expectedNextGrowthSeed)
    {
        Array.Resize(ref rates, 9);
        var currentStats = Enumerable.Range(10, 9).ToArray();
        var statCaps = Enumerable.Repeat(100, 9).ToArray();
        capOverrides.CopyTo(statCaps, 0);

        var result = RandomLevelUp.Calculate(growthSeed, rates, currentStats, statCaps, (byte)targetStatCount);

        CollectionAssert.AreEqual(expectedGains.Select(digit => digit - '0').ToArray(), result.StatGains.ToArray());
        Assert.AreEqual(expectedNextGrowthSeed, result.NextGrowthSeed);
    }

    [DataTestMethod]
    [DataRow(8, 9, 9)]
    [DataRow(9, 10, 9)]
    [DataRow(9, 9, 8)]
    public void RequiresNineValuesForEachStatArray(int rateCount, int currentCount, int capCount)
    {
        Assert.ThrowsException<ArgumentException>(() =>
            RandomLevelUp.Calculate(0, new short[rateCount], new int[currentCount], new int[capCount], 0));
    }

    [TestMethod]
    public void ReproducesSixConsecutiveLevels()
    {
        uint growthSeed = 123456789;
        var rates = Enumerable.Repeat((short)50, 9).ToArray();
        var currentStats = Enumerable.Range(10, 9).ToArray();
        var statCaps = Enumerable.Repeat(100, 9).ToArray();
        string[] expected = ["101011100", "000001001", "001001001", "001110111", "100010100", "000111001"];

        foreach (string gains in expected)
        {
            var result = RandomLevelUp.Calculate(growthSeed, rates, currentStats, statCaps, 1);
            Assert.AreEqual(gains, string.Concat(result.StatGains));
            for (int i = 0; i < currentStats.Length; i++)
                currentStats[i] += result.StatGains[i];
            growthSeed = result.NextGrowthSeed;
        }
    }
}
