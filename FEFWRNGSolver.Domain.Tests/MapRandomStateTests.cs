using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Domain.Tests;

[TestClass]
public sealed class MapRandomStateTests
{
    // 各スロットの Random ブロックから読み取った、レダ編第2部の状態。
    private static readonly MapRandomState PartStart = new(0x9C841526, 0x6B143AD2, 0x2B9F020D, 0x2D883741);
    private static readonly MapRandomState ChapterOne = new(0x6B143AD2, 0x2B9F020D, 0x2D883741, 0x91193AF3);
    private static readonly MapRandomState ChapterTwo = new(0x3996122A, 0xF8E5CC02, 0xB4ED2769, 0xA9BC4E27);

    [TestMethod]
    public void NextMatchesSavedStateIncludingHighBit()
    {
        Assert.AreEqual(ChapterOne, PartStart.Next());
    }

    [TestMethod]
    public void AdvanceResumesFromSavedState()
    {
        Assert.AreEqual(ChapterTwo, ChapterOne.Advance(1085));
        Assert.AreEqual(ChapterTwo, PartStart.Advance(1086));
    }

    [TestMethod]
    public void ZeroDrawsPreserveState()
    {
        Assert.AreEqual(PartStart, PartStart.Advance(0));
    }

    [TestMethod]
    public void RejectsNegativeDrawCount()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => PartStart.Advance(-1));
    }
}
