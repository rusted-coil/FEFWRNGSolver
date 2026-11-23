using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class PredictionClassTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PredictsEachSelectedClassAtEachAdvanceUsingItsOwnRates(bool filter)
    {
        const string soldier = "JID_兵士";
        const string rider = "JID_軽騎兵";
        const string charioteer = "JID_戦車兵";
        string[] classIds = [soldier, rider, charioteer];
        var model = SetUp(charioteer);
        model.ConsiderClassChanges = true;
        foreach (var id in classIds.AsEnumerable().Reverse())
            model.SetPredictionClassSelected(id, true);
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);

        var expected = new List<LevelUpRow>();
        foreach (var id in classIds)
        {
            var single = SetUp(id);
            Assert.IsTrue(single.Calculate(), single.ErrorMessage);
            Assert.IsTrue(single.Results.Count > 0);
            Assert.IsTrue(single.Results.All(row => row.ClassId == id));
            expected.AddRange(single.Results);
        }
        if (filter)
            Assert.IsTrue(expected.Count < 26 * classIds.Length);
        else
            Assert.AreEqual(26 * classIds.Length, expected.Count);
        Assert.AreEqual(expected.Count, model.Results.Count);
        foreach (var (reference, actual) in expected.OrderBy(row => row.Advances).Zip(model.Results))
        {
            Assert.AreEqual(reference.Advances, actual.Advances);
            Assert.AreEqual(reference.ClassId, actual.ClassId);
            Assert.AreEqual(reference.GrowthSeed, actual.GrowthSeed);
            Assert.AreEqual(reference.NextGrowthSeed, actual.NextGrowthSeed);
            CollectionAssert.AreEqual(reference.StatGains.ToArray(), actual.StatGains.ToArray());
        }
        Assert.AreEqual(charioteer, model.Setup.ClassId);
        Assert.AreEqual("汗血馬", model.Setup.AnimalId);
        Assert.AreEqual(GrowthSkillBonus.CharioteersPath35, model.SelectedSkillBonus);
        Assert.AreEqual("250", model.GrowthRates[0].Rate);

        MainViewModel SetUp(string id)
        {
            var settings = new MainViewModel { MaxAdvances = "25" };
            settings.Setup.UnitId = "PID_ムウ";
            settings.Setup.ClassId = id;
            if (id is rider or charioteer)
            {
                settings.Setup.AnimalId = "汗血馬";
                settings.Setup.Affection = 3;
            }
            if (id == charioteer)
            {
                settings.SelectedSkillBonus = GrowthSkillBonus.CharioteersPath35;
                settings.GrowthRates[0].Rate = "250";
            }
            if (filter)
            {
                settings.GrowthRates[1].Condition = StatGrowthCondition.MustGrow;
                settings.GrowthRates[8].Condition = StatGrowthCondition.Capped;
                settings.MaxGrowthCount = 5;
            }
            settings.GrowthSeed = "075BCD15";
            return settings;
        }
    }
}
