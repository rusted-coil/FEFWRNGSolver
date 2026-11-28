using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class LocalizationCatalogTests
{
    private const string Languages = """
        [
          {"code":"jp","displayName":"日本語","matchCodes":["ja"]},
          {"code":"en","displayName":"English","matchCodes":["en"]},
          {"code":"fr","displayName":"Français","matchCodes":["fr"]},
          {"code":"qc","displayName":"Français (Canada)","matchCodes":["fr-CA"]}
        ]
        """;

    [DataTestMethod]
    [DataRow(null, "ja-JP", "fr", "jp")]
    [DataRow(null, "de-DE", "fr-FR", "fr")]
    [DataRow(null, "fr-CA", "en", "qc")]
    [DataRow(null, "es-ES", "de-DE", "en")]
    [DataRow("jp", "en-US", "en", "jp")]
    [DataRow("removed", "ja-JP", "en", "jp")]
    public async Task ChoosesSavedOrFirstSupportedPreferredLanguage(string? saved, string first, string second, string expected)
    {
        var catalog = await LocalizationCatalog.LoadAsync(
            path => Task.FromResult(path == "languages.json" ? Languages : null), saved, [first, second]);
        Assert.AreEqual(expected, catalog.Code);
        CollectionAssert.AreEqual(new[] { "日本語", "English", "Français", "Français (Canada)" },
            catalog.Languages.Select(language => language.DisplayName).ToArray());
    }

    [TestMethod]
    public async Task ResolvesEachMissingTranslationInItsOwnSection()
    {
        var files = new Dictionary<string, string>
        {
            ["languages.json"] = Languages,
            ["fr/system.json"] = """{"SYS_SETTINGS":"Paramètres","SYS_LANGUAGE":" ","SYS_COUNT":"{9}"}""",
            ["en/system.json"] = """{"SYS_SETTINGS":"Settings","SYS_LANGUAGE":"Language","SYS_COUNT":"Count {0}"}""",
            ["en/character.json"] = """{"ID":"English unit"}""",
            ["jp/character.json"] = """{"ID":"日本語ユニット","MISSING_EN":"日本語のみ"}""",
            ["fr/job.json"] = "(invalid JSON)",
            ["jp/job.json"] = """{"ID":"日本語兵種"}"""
        };
        var catalog = await LocalizationCatalog.LoadAsync(path => Task.FromResult(files.GetValueOrDefault(path)), "fr", []);
        Assert.AreEqual("Paramètres", catalog["SYS_SETTINGS"]);
        Assert.AreEqual("Language", catalog["SYS_LANGUAGE"]);
        Assert.AreEqual("Count 2", catalog.Format("SYS_COUNT", 2));
        Assert.AreEqual("English unit", catalog.Character("ID"));
        Assert.AreEqual("日本語のみ", catalog.Character("MISSING_EN"));
        Assert.AreEqual("日本語兵種", catalog.Job("ID"));
        Assert.AreEqual("ID", catalog.Skill("ID"));
    }

    [TestMethod]
    public async Task SwitchingLanguagePreservesSelectionEditsResultsAndValidation()
    {
        var catalog = await LoadBundledCatalog("jp");
        var model = new SeedSearchViewModel(new TestStore(), catalog);
        var prediction = model.Settings;
        prediction.Setup.UnitId = "PID_カイ";
        prediction.Setup.ClassId = "JID_戦車兵";
        prediction.Setup.AnimalId = "汗血馬";
        prediction.SelectedSkillBonus = GrowthSkillBonus.CharioteersPath35;
        foreach (var stat in prediction.GrowthRates)
            stat.Rate = "100";
        prediction.GrowthSeed = "1234ABCD";
        Assert.IsTrue(prediction.Calculate());
        var results = prediction.Results;
        await model.SearchAsync();
        Assert.IsTrue(model.HasCompleted);
        var matches = model.Matches;
        prediction.GrowthRates[1].Rate = "x";
        Assert.IsFalse(prediction.Calculate());
        string? japaneseError = prediction.ErrorMessage;

        catalog.Select("en");
        Assert.AreEqual("Cai", prediction.Setup.Units.Single(unit => unit.Id == prediction.Setup.UnitId).Name);
        Assert.AreEqual(catalog.Job("JID_戦車兵"), prediction.Setup.Classes.Single(job => job.Id == prediction.Setup.ClassId).Name);
        Assert.AreEqual("汗血馬", prediction.Setup.AnimalId);
        Assert.AreEqual(GrowthSkillBonus.CharioteersPath35, prediction.SelectedSkillBonus);
        Assert.AreEqual("1234ABCD", prediction.GrowthSeed);
        Assert.AreEqual("x", prediction.GrowthRates[1].Rate);
        Assert.AreSame(results, prediction.Results);
        Assert.AreSame(matches, model.Matches);
        Assert.IsTrue(model.HasCompleted);
        Assert.AreNotEqual(japaneseError, prediction.ErrorMessage);
        StringAssert.Contains(prediction.ErrorMessage!, prediction.GrowthRates[1].Name);
    }

    [DataTestMethod]
    [DataRow("en", "Eshmel", "Eshmel(Male)", "Eshmel(Female)")]
    [DataRow("jp", "イシュマール", "イシュマール（男）", "イシュマール（女）")]
    public async Task DistinguishesBothFormsInThePickerWhileKeepingTheGameName(string code, string name, string male, string female)
    {
        var catalog = await LoadBundledCatalog(code);
        var units = new MainViewModel(new TestStore(), catalog).Setup.Units;
        Assert.AreEqual(name, catalog.Character("PID_イシュマール/M"));
        Assert.AreEqual(name, catalog.Character("PID_イシュマール/F"));
        Assert.AreEqual(male, units.Single(u => u.Id == "PID_イシュマール/M").Name);
        Assert.AreEqual(female, units.Single(u => u.Id == "PID_イシュマール/F").Name);
    }

    [TestMethod]
    public async Task ExportsAllClassesInTheDisplayLanguageWithTheCalculatedSeedAndBaseClass()
    {
        var catalog = await LocalizationCatalog.LoadAsync(path => Task.FromResult<string?>(path == "en/job.json"
            ? """{"JID_戦車兵":"Charioteer","JID_剣士":"Sword, \"light\"\ninfantry"}"""
            : File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "languages", path))), "jp", []);
        var model = new MainViewModel(new TestStore(), catalog);
        model.Setup.UnitId = "PID_ムウ";
        model.Setup.ClassId = "JID_戦車兵";
        model.ConsiderClassChanges = true;
        model.SetPredictionClassSelected("JID_剣士", true);
        model.SetPredictionClassSelected("JID_戦車兵", true);
        model.GrowthSeed = "0000000a";
        model.MaxAdvances = "200";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);

        model.Setup.ClassId = "JID_剣士";
        model.GrowthSeed = "";
        Assert.IsFalse(model.Calculate());
        catalog.Select("en");
        Assert.AreEqual("Mu_0000000A_Charioteer.csv", model.PredictionCsvFileName);
        using var csv = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(model.CreatePredictionCsv()));
        csv.SetDelimiters(",");
        Assert.AreEqual("Advances", csv.ReadFields()![0]);
        Assert.AreEqual(402, model.Results.Count);
        foreach (var row in model.Results)
        {
            var fields = csv.ReadFields()!;
            Assert.AreEqual(14, fields.Length);
            Assert.AreEqual(catalog.Job(row.ClassId), fields[1]);
            Assert.AreEqual(row.GrowthSeed, fields[2]);
        }
        Assert.IsTrue(csv.EndOfData);
    }

    private static Task<LocalizationCatalog> LoadBundledCatalog(string code) =>
        LocalizationCatalog.LoadAsync(path => Task.FromResult<string?>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "languages", path))), code, []);

    private sealed class TestStore : IUnitSettingsStore
    {
        public UnitSettings? Load(string unitId) => null;
        public void Save(string unitId, UnitSettings settings) { }
    }
}
