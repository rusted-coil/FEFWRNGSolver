using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class UnitSettingsTests
{
    private const string Female = "PID_イシュマール/F";
    private const string Male = "PID_イシュマール/M";

    [TestMethod]
    public void SavesEachSettingImmediatelyAndRestoresByUnitId()
    {
        var store = new JsonStore();
        var model = new SeedSearchViewModel(store).Settings;
        model.Setup.UnitId = Female;
        model.Setup.ClassId = "JID_戦車兵";
        Assert.AreEqual(model.Setup.ClassId, store.Load(Female)!.ClassId);
        model.Setup.AnimalId = "野生の馬";
        Assert.AreEqual(model.Setup.AnimalId, store.Load(Female)!.AnimalId);
        model.Setup.Affection = 2;
        Assert.AreEqual(2, store.Load(Female)!.Affection);
        model.SelectedSkillBonus = GrowthSkillBonus.CharioteersPath35;
        Assert.AreEqual(GrowthSkillBonus.CharioteersPath35, store.Load(Female)!.SkillBonus);
        Assert.IsNull(store.Load(Female)!.GrowthSeed);

        model.AdoptGrowthSeed(0);
        Assert.AreEqual("00000000", model.GrowthSeed);
        model.Setup.UnitId = Male;
        Assert.AreEqual("", model.GrowthSeed);
        Assert.AreEqual("", model.Setup.AnimalId);
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
        model.GrowthSeed = "deadbeef";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);

        var reopened = new MainViewModel(store);
        string? seedAtSelection = null;
        reopened.UnitSelected += () => seedAtSelection = reopened.GrowthSeed;
        reopened.Setup.UnitId = Female;
        Assert.AreEqual("00000000", seedAtSelection);
        Assert.AreEqual("JID_戦車兵", reopened.Setup.ClassId);
        Assert.AreEqual("野生の馬", reopened.Setup.AnimalId);
        Assert.AreEqual(2, reopened.Setup.Affection);
        Assert.AreEqual(GrowthSkillBonus.CharioteersPath35, reopened.SelectedSkillBonus);
        reopened.Setup.ClassId = "JID_兵士";
        Assert.AreEqual(GrowthSkillBonus.None, store.Load(Female)!.SkillBonus);
        reopened.Setup.UnitId = Male;
        Assert.AreEqual("DEADBEEF", seedAtSelection);
    }

    [TestMethod]
    public void IgnoresSavedSkillFromAnIncompatibleClass()
    {
        var store = new JsonStore();
        store.Save(Female, new("JID_戦車兵", "", 5, GrowthSkillBonus.ElephantRidersPath45, 0xA));
        var model = new MainViewModel(store);
        model.Setup.UnitId = Female;
        Assert.AreEqual("JID_戦車兵", model.Setup.ClassId);
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
        Assert.AreEqual("0000000A", model.GrowthSeed);
    }

    [TestMethod]
    public void SavesOnlyAdoptedOrValidSubmittedSeeds()
    {
        var store = new JsonStore();
        var model = new MainViewModel(store);
        model.Setup.UnitId = Female;
        model.AdoptGrowthSeed(0xA);
        model.GrowthSeed = "0000000B";
        model.Setup.Affection = 1;
        Assert.AreEqual(0xAu, store.Load(Female)!.GrowthSeed);
        model.GrowthSeed = "invalid";
        Assert.IsFalse(model.Calculate());
        Assert.AreEqual(0xAu, store.Load(Female)!.GrowthSeed);

        model.GrowthSeed = "0000000b";
        model.LevelUpCount = "0";
        Assert.IsFalse(model.Calculate());
        Assert.AreEqual(0xBu, store.Load(Female)!.GrowthSeed);
        model.Setup.UnitId = "";
        model.AdoptGrowthSeed(0);
        model.LevelUpCount = "1";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.IsNull(store.Load(""));
        model.Setup.UnitId = Female;
        Assert.AreEqual("0000000B", model.GrowthSeed);
        Assert.AreEqual(0, model.Results.Count);
    }

    private sealed class JsonStore : IUnitSettingsStore
    {
        private readonly Dictionary<string, string> m_Json = [];

        public UnitSettings? Load(string unitId) =>
            m_Json.TryGetValue(unitId, out var json) ? UnitSettingsJson.Deserialize(json) : null;

        public void Save(string unitId, UnitSettings settings) =>
            m_Json[unitId] = UnitSettingsJson.Serialize(settings);
    }
}
