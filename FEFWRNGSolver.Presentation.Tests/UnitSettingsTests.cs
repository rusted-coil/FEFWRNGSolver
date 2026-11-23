using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class UnitSettingsTests
{
    private const string Female = "PID_イシュマール/F";
    private const string Male = "PID_イシュマール/M";

    [TestMethod]
    public void OnlyListedUnitsAreExemptFromThePrePartThreeGuarantee()
    {
        var model = new MainViewModel();
        var exemptUnits = new List<string>();
        foreach (var unit in GrowthRateMasterData.Default.UnitRoster)
        {
            model.Setup.UnitId = unit.Id;
            if (!model.CanSelectPartThree)
                exemptUnits.Add(unit.Id);
            Assert.AreEqual(model.CanSelectPartThree ? "2" : "0", model.GuaranteedStatCount, unit.Id);
            model.IsPartThree = true;
            Assert.AreEqual(model.CanSelectPartThree, model.IsPartThree, unit.Id);
            Assert.AreEqual("0", model.GuaranteedStatCount, unit.Id);
        }
        string[] expected = [Male, Female, "PID_コウカ", "PID_トロイア", "PID_キリーク", "PID_ネイサン",
            "PID_セントリオン", "PID_アスワン", "PID_タホウニア", "PID_クラプカ"];
        CollectionAssert.AreEquivalent(expected, exemptUnits);
    }

    [TestMethod]
    public async Task AppliesPartThreeGuaranteeAndRestoresTheFlagPerUnit()
    {
        var unit = new UnitGrowthData("eligible", "対象", UnitGender.Female, new(1, 2, 3), HasPrePartThreeGuarantee: true);
        var master = new GrowthRateMasterData([unit, unit with { Id = "other", HasPrePartThreeGuarantee = false }],
            [new("job", "兵種", default)], []);
        var text = await LocalizationCatalog.LoadAsync(_ => Task.FromResult<string?>(null), "jp", []);
        var store = new JsonStore();
        var legacy = UnitSettingsJson.Deserialize("""{"ClassId":"job","AnimalId":"","Affection":5,"SkillBonus":0,"GrowthSeed":123456789}""")!;
        store.Save(unit.Id, legacy);
        store.Save("other", legacy with { IsPartThree = true });
        var model = new MainViewModel(store, text, master) { MaxAdvances = "0", GuaranteedStatCount = "3" };
        model.Setup.UnitId = unit.Id;
        model.GuaranteedStatCount = "8";
        Assert.AreEqual("2", model.GuaranteedStatCount);
        Assert.IsTrue(model.CanSelectPartThree);
        Assert.IsFalse(model.IsPartThree);
        Assert.IsTrue(model.Calculate());
        Assert.AreEqual(2, model.Results.Single().StatGains.Sum());
        model.IsPartThree = true;
        Assert.AreEqual("0", model.GuaranteedStatCount);
        Assert.IsTrue(store.Load(unit.Id)!.IsPartThree);
        Assert.IsTrue(model.Calculate());
        Assert.AreEqual(0, model.Results.Single().StatGains.Sum());

        model.Setup.UnitId = "other";
        Assert.IsFalse(model.CanSelectPartThree);
        model.IsPartThree = true;
        Assert.IsFalse(model.IsPartThree);
        Assert.IsTrue(model.Calculate());
        Assert.AreEqual(0, model.Results.Single().StatGains.Sum());
        model.Setup.UnitId = unit.Id;
        Assert.IsTrue(model.IsPartThree);
        model.IsPartThree = false;
        Assert.IsFalse(store.Load(unit.Id)!.IsPartThree);
        model.Setup.UnitId = "";
        Assert.AreEqual("3", model.GuaranteedStatCount);
    }

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
    public void RestoresPredictionClassesAndFlagIndependentlyForEachUnit()
    {
        const string pegasus = "JID_天翼兵";
        const string soldier = "JID_兵士";
        var store = new JsonStore();
        var old = UnitSettingsJson.Deserialize("""{"ClassId":"JID_兵士","AnimalId":"","Affection":5,"SkillBonus":0,"GrowthSeed":123}""")!;
        store.Save(Female, old);
        var model = new MainViewModel(store);
        model.Setup.UnitId = Female;
        Assert.IsFalse(model.ConsiderClassChanges);
        Assert.IsFalse(model.Setup.Classes.Any(job => model.IsPredictionClassSelected(job.Id)));
        model.ConsiderClassChanges = true;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.IsTrue(model.HasCalculated);
        Assert.AreEqual(0, model.Results.Count);
        model.SetPredictionClassSelected(pegasus, true);
        model.SetPredictionClassSelected(soldier, true);
        model.SetPredictionClassSelected(soldier, false);

        model.Setup.UnitId = Male;
        Assert.IsFalse(model.ConsiderClassChanges);
        Assert.IsFalse(model.Setup.Classes.Any(job => model.IsPredictionClassSelected(job.Id)));
        model.SetPredictionClassSelected(soldier, true);
        var reopened = new MainViewModel(store);
        reopened.Setup.UnitId = Female;
        Assert.IsTrue(reopened.ConsiderClassChanges);
        Assert.IsTrue(reopened.IsPredictionClassSelected(pegasus));
        Assert.IsFalse(reopened.IsPredictionClassSelected(soldier));
        reopened.Setup.UnitId = Male;
        Assert.IsFalse(reopened.ConsiderClassChanges);
        Assert.IsTrue(reopened.IsPredictionClassSelected(soldier));
        Assert.IsFalse(reopened.IsPredictionClassSelected(pegasus));
    }

    [TestMethod]
    public void ListsAllTiersAndIgnoresUnavailableOrUnknownSavedClasses()
    {
        const string pegasus = "JID_天翼兵";
        const string soldier = "JID_兵士";
        var store = new JsonStore();
        store.Save(Male, new("JID_闘士", "", 5, GrowthSkillBonus.None, 0)
        {
            ConsiderClassChanges = true, PredictionClassIds = [pegasus, soldier, "unknown"]
        });
        var model = new MainViewModel(store) { MaxAdvances = "0" };
        model.Setup.UnitId = Male;
        var groups = model.Setup.PredictionClassGroups;
        CollectionAssert.AreEqual(new[] { "下級職", "中級職", "上級職", "最上級職", "神将職" },
            groups.Select(group => group.Name).ToArray());
        var options = groups.SelectMany(group => group.Classes).ToArray();
        CollectionAssert.AreEquivalent(GrowthRateMasterData.Default.Classes.Keys.ToArray(), options.Select(job => job.Id).ToArray());
        Assert.IsFalse(groups[1].Classes.Single(job => job.Id == pegasus).IsAvailable);
        Assert.IsTrue(groups[0].Classes.Single(job => job.Id == soldier).IsAvailable);
        Assert.IsFalse(model.IsPredictionClassSelected(pegasus));
        Assert.IsFalse(model.IsPredictionClassSelected("unknown"));
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(soldier, model.Results.Single().ClassId);
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
        model.MaxAdvances = "-1";
        Assert.IsFalse(model.Calculate());
        Assert.AreEqual(0xBu, store.Load(Female)!.GrowthSeed);
        model.Setup.UnitId = "";
        model.AdoptGrowthSeed(0);
        model.MaxAdvances = "0";
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
