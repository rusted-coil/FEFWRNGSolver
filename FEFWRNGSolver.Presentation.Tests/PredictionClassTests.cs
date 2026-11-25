using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class PredictionClassTests
{
    private const string Charioteer = "JID_戦車兵";
    private const string Swordsman = "JID_剣士";
    private const string Horse = "汗血馬";

    [TestMethod]
    public void PredictsEquippedCharioteerAlongsideCurrentSwordsman()
    {
        var model = CreateModel(Swordsman);
        model.ConsiderClassChanges = true;
        model.SetPredictionClassSelected(Swordsman, true);
        model.SetPredictionClassSelected(Charioteer, true);
        model.SetPredictionSettings(Charioteer, new(GrowthSkillBonus.CharioteersPath45, Horse));

        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        AssertCandidateMatches(model, CreateModel(Swordsman));
        AssertCandidateMatches(model, CreateModel(Charioteer, GrowthSkillBonus.CharioteersPath45, Horse, 5));
        Assert.AreEqual(Swordsman, model.Setup.ClassId);
        Assert.AreEqual("", model.Setup.AnimalId);
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
    }

    [TestMethod]
    public void ResolvesEachOverrideIndependentlyAndRespectsManualRatesWhenInherited()
    {
        var model = CreateModel(Charioteer, GrowthSkillBonus.CharioteersPath45, Horse, 2);
        model.GrowthRates[0].Rate = "250";
        model.ConsiderClassChanges = true;
        model.SetPredictionClassSelected(Charioteer, true);

        Check(new(SkillBonus: GrowthSkillBonus.None), GrowthSkillBonus.None, Horse, 2);
        Check(new(AnimalId: ""), GrowthSkillBonus.CharioteersPath45, "", 5);
        Check(new(AnimalId: Horse, Affection: 0), GrowthSkillBonus.CharioteersPath45, Horse, 0);
        Check(new(), GrowthSkillBonus.CharioteersPath45, Horse, 2, manual: true);

        model.ConsiderClassChanges = false;
        Check(new(GrowthSkillBonus.None, ""), GrowthSkillBonus.CharioteersPath45, Horse, 2, manual: true);
        Assert.AreEqual("250", model.GrowthRates[0].Rate);

        void Check(PredictionClassSettings settings, GrowthSkillBonus skill, string animal, int affection, bool manual = false)
        {
            model.SetPredictionSettings(Charioteer, settings);
            var single = CreateModel(Charioteer, skill, animal, affection);
            if (manual)
                single.GrowthRates[0].Rate = "250";
            Assert.IsTrue(model.Calculate(), model.ErrorMessage);
            AssertCandidateMatches(model, single);
        }
    }

    private static MainViewModel CreateModel(string classId, GrowthSkillBonus skill = GrowthSkillBonus.None,
        string animal = "", int affection = 5)
    {
        var model = new MainViewModel { MaxAdvances = "64" };
        model.Setup.UnitId = "PID_ムウ";
        model.Setup.ClassId = classId;
        model.Setup.AnimalId = animal;
        model.Setup.Affection = affection;
        model.SelectedSkillBonus = skill;
        model.GrowthSeed = "075BCD15";
        return model;
    }

    private static void AssertCandidateMatches(MainViewModel model, MainViewModel single)
    {
        Assert.IsTrue(single.Calculate(), single.ErrorMessage);
        var rows = model.Results.Where(row => row.ClassId == single.Setup.ClassId).ToArray();
        Assert.AreEqual(single.Results.Count, rows.Length);
        foreach (var (expected, actual) in single.Results.Zip(rows))
        {
            Assert.AreEqual(expected.Advances, actual.Advances);
            CollectionAssert.AreEqual(expected.StatGains.ToArray(), actual.StatGains.ToArray());
        }
    }

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
            Assert.AreEqual(reference.MinimumExamPassRate, actual.MinimumExamPassRate);
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
