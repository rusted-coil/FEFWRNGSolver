using System.Collections.Immutable;
using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.App.Tests;

[TestClass]
public sealed class LevelUpSimulationTests
{
    [TestMethod]
    public void KeepsSelectedSkillBonusForEveryLevelUp()
    {
        var setup = new UnitGrowthSetup("PID_ムウ", "JID_戦車兵", SkillBonus: GrowthSkillBonus.CharioteersPath35);
        var steps = LevelUpSimulation.Calculate(0, GrowthRateMasterData.Default, setup, levelUpCount: 46);

        ImmutableArray<short> expectedRates = [70, 60, 25, 70, 50, 35, 60, 30, 55];
        var expected = LevelUpSimulation.Calculate(0, expectedRates, levelUpCount: 46, cappedStatIndices: [], targetStatCount: 2);
        CollectionAssert.AreEqual(expected.SelectMany(step => step.StatGains).ToArray(),
            steps.SelectMany(step => step.StatGains).ToArray());
    }
}
