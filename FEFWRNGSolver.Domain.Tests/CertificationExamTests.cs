using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Domain.Tests;

[TestClass]
public sealed class CertificationExamTests
{
    [DataTestMethod]
    [DataRow(2_275_099u, 1)]   // 2個目の乱数を1,000,000で割った余りは0
    [DataRow(1_269_780u, 50)]  // 499,999
    [DataRow(961_941u, 51)]    // 500,000
    [DataRow(54_519u, 100)]    // 999,999
    public void ReturnsLowestPassingWholePercent(uint growthSeed, int expectedRate)
    {
        Assert.AreEqual(expectedRate, CertificationExam.GetMinimumPassRate(growthSeed));
    }
}
