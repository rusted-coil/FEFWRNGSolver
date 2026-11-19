using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Domain.Tests;

[TestClass]
public sealed class GrowthRateMasterDataTests
{
    [DataTestMethod]
    [DataRow("JID_戦車兵", GrowthSkillBonus.None, new short[] { 60, 55, 20, 65, 45, 35, 50, 30, 50 })]
    [DataRow("JID_戦車兵", GrowthSkillBonus.CharioteersPath35, new short[] { 70, 60, 25, 70, 50, 35, 60, 30, 55 })]
    [DataRow("JID_戦車兵", GrowthSkillBonus.CharioteersPath45, new short[] { 70, 70, 25, 75, 50, 40, 70, 35, 60 })]
    [DataRow("JID_戦象兵", GrowthSkillBonus.None, new short[] { 70, 60, 20, 70, 40, 35, 55, 25, 50 })]
    [DataRow("JID_戦象兵", GrowthSkillBonus.ElephantRidersPath45, new short[] { 80, 70, 20, 80, 40, 35, 65, 35, 50 })]
    public void AppliesOnlyTheSelectedSkillToMusGrowthRates(string classId, GrowthSkillBonus skill, short[] expected)
    {
        var setup = new UnitGrowthSetup("PID_ムウ", classId, SkillBonus: skill);
        var rates = GrowthRateMasterData.Default.Calculate(setup);

        CollectionAssert.AreEqual(expected, rates.ToArray());
    }

    [DataTestMethod]
    [DataRow(null, 0)]
    [DataRow(0, 1)]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 4)]
    [DataRow(4, 5)]
    [DataRow(5, 6)]
    public void MultipliesOnlyTheAnimalBonusBeforeClamping(int? affection, int bonus)
    {
        var animal = new AnimalGrowthData("horse", "馬", AnimalType.Horse,
            new(1, 1), new(2, 2), new(3, 3), new(4, 4), new(5, 5), new(6, 6));
        AssignedAnimal? assignment = affection.HasValue ? new(animal.Id, affection.Value) : null;
        foreach (int multiplier in new[] { 1, 2 })
        {
            var master = new GrowthRateMasterData([new("unit", "ユニット", UnitGender.Female, new(Hp: 80, Strength: 1))],
                [new("class", "兵種", new(Hp: 10, Strength: -9), AllowedAnimalType: AnimalType.Horse, AnimalMultiplier: multiplier)], [animal]);
            var setup = new UnitGrowthSetup("unit", "class", assignment, GrowthSkillBonus.CharioteersPath35);
            var rates = master.Calculate(setup);

            Assert.AreEqual(100 + multiplier * bonus, (int)rates[0]);
            Assert.AreEqual(Math.Max(0, -3 + multiplier * bonus), (int)rates[1]);
        }
    }

    [DataTestMethod]
    [DataRow(5)]
    [DataRow(7)]
    public void RequiresExactlySixAffectionStages(int length) =>
        Assert.ThrowsException<ArgumentException>(() =>
            new AnimalGrowthData("horse", "馬", AnimalType.Horse, new StatGrowthRates[length]));

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(6)]
    public void RejectsAffectionOutsideTheTable(int affection)
    {
        var master = GrowthRateMasterData.Default;
        var setup = new UnitGrowthSetup("PID_ムウ", "JID_軽騎兵", new("汗血馬", affection));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => master.Calculate(setup));
    }

    [TestMethod]
    public void ChecksGenderForFemaleOnlyClasses()
    {
        var master = GrowthRateMasterData.Default;
        var male = master.Units["PID_イシュマール/M"];
        var female = master.Units["PID_イシュマール/F"];
        Assert.AreEqual(male.PersonalRates, female.PersonalRates);
        var wingSoldier = master.Classes["JID_天翼兵"];
        Assert.IsFalse(wingSoldier.IsAvailableTo(male));
        Assert.IsTrue(wingSoldier.IsAvailableTo(female));
        CollectionAssert.AreEqual(new short[] { 60, 45, 50, 50, 45, 45, 40, 40, 60 },
            master.Calculate(new(female.Id, wingSoldier.Id)).ToArray());
        Assert.ThrowsException<ArgumentException>(() => master.Calculate(new(male.Id, wingSoldier.Id)));
    }

    [TestMethod]
    public void EnforcesDeploymentTypeForMountedAndUnmountedClasses()
    {
        var master = GrowthRateMasterData.Default;
        var assignment = new AssignedAnimal("汗血馬", 5);
        CollectionAssert.AreEqual(new short[] { 65, 60, 20, 55, 55, 35, 55, 30, 50 },
            master.Calculate(new("PID_ムウ", "JID_軽騎兵", assignment)).ToArray());
        CollectionAssert.AreEqual(new short[] { 70, 65, 20, 75, 45, 35, 70, 30, 50 },
            master.Calculate(new("PID_ムウ", "JID_戦車兵", assignment)).ToArray());
        Assert.ThrowsException<ArgumentException>(() => master.Calculate(new("PID_ムウ", "JID_兵士", assignment)));
        Assert.ThrowsException<ArgumentException>(() => master.Calculate(new("PID_ムウ", "JID_天翼兵", assignment)));
    }
}
