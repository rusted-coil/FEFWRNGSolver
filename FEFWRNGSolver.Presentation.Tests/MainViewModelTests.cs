using FEFWRNGSolver.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class MainViewModelTests
{
    [TestMethod]
    public void ListsManualInputFirstAndUnitsInJapaneseNameOrder()
    {
        var units = new MainViewModel().Setup.Units;
        Assert.AreEqual(new GrowthOption("", "指定無し"), units[0]);
        string[] expectedOrder = ["アスワン", "ウーシュラ", "ウルタンド", "ジェスター", "タリムーン", "ロレッタ"];
        var displayedOrder = units.Skip(1).Select(unit => unit.Name).Where(expectedOrder.Contains).ToArray();
        CollectionAssert.AreEqual(expectedOrder, displayedOrder);
    }

    [TestMethod]
    public void UsesSelectedRatesForPredictionAndRestoresManualInput()
    {
        var model = CreateModel("123");
        var setup = model.Setup;
        setup.UnitId = "PID_ムウ";
        model.GrowthSeed = "075bcd15";
        setup.ClassId = "JID_戦車兵";
        setup.AnimalId = "汗血馬";
        Assert.AreEqual(5, setup.Affection);
        model.SelectedSkillBonus = GrowthSkillBonus.CharioteersPath35;

        short[] expected = [80, 70, 25, 50, 80, 80, 30, 35, 55];
        CollectionAssert.AreEqual(expected.Select(rate => rate.ToString()).ToArray(),
            model.GrowthRates.Select(stat => stat.Rate).ToArray());
        var manual = CreateModel(expected.Select(rate => rate.ToString()).ToArray());
        manual.GuaranteedStatCount = model.GuaranteedStatCount;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.IsTrue(manual.Calculate(), manual.ErrorMessage);
        CollectionAssert.AreEqual(manual.Results[0].StatGains.ToArray(), model.Results[0].StatGains.ToArray());

        model.GrowthRates[0].Rate = "200";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(2, model.Results[0].StatGains[0]);
        setup.Affection = 4;
        Assert.AreEqual(model.SelectedGrowthRates[0].ToString(), model.GrowthRates[0].Rate);

        setup.UnitId = "";
        Assert.IsTrue(setup.IsManual);
        Assert.IsFalse(setup.HasAnimal);
        Assert.AreEqual(GrowthSkillBonus.None, model.SkillOptions.Single().Value);
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
        Assert.AreEqual("123", model.GrowthRates[0].Rate);
    }

    [TestMethod]
    public void ClearsSelectionsWhenGenderOrAnimalTypeBecomesIncompatible()
    {
        var setup = new MainViewModel().Setup;
        setup.UnitId = "PID_イシュマール/F";
        setup.ClassId = "JID_天翼兵";
        setup.AnimalId = "野生のペガサス";
        setup.Affection = 4;
        setup.AnimalId = "ダークペガサス";
        Assert.AreEqual(4, setup.Affection);
        setup.ClassId = "JID_戦車兵";
        Assert.AreEqual("", setup.AnimalId);
        Assert.AreEqual(4, setup.Affection);
        Assert.IsTrue(setup.Animals.Any(animal => animal.Id == "汗血馬"));

        setup.ClassId = "JID_天翼兵";
        setup.UnitId = "PID_イシュマール/M";
        Assert.AreNotEqual("JID_天翼兵", setup.ClassId);
        Assert.IsFalse(setup.Classes.Any(job => job.Id == "JID_天翼兵"));
        Assert.IsTrue(setup.Classes.Any(job => job.Id == setup.ClassId));
    }

    [TestMethod]
    public void LimitsSkillOptionsToTheCurrentClassAndClearsIncompatibleBonuses()
    {
        var model = new MainViewModel();
        model.Setup.UnitId = "PID_ムウ";
        model.Setup.ClassId = "JID_戦車兵";
        CollectionAssert.AreEqual(new[] { "スキル補正無し", "戦車兵の道（Lv35）", "戦車兵の道（Lv45）" },
            model.SkillOptions.Select(option => option.Name).ToArray());
        model.SelectedSkillBonus = GrowthSkillBonus.CharioteersPath45;
        Assert.AreEqual("70", model.GrowthRates[0].Rate);

        model.Setup.ClassId = "JID_戦象兵";
        CollectionAssert.AreEqual(new[] { "スキル補正無し", "戦象兵の道（Lv45）" },
            model.SkillOptions.Select(option => option.Name).ToArray());
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
        Assert.AreEqual("70", model.GrowthRates[0].Rate);
        model.SelectedSkillBonus = GrowthSkillBonus.ElephantRidersPath45;
        Assert.AreEqual("80", model.GrowthRates[0].Rate);

        model.Setup.ClassId = "JID_軽騎兵";
        Assert.AreEqual(GrowthSkillBonus.None, model.SkillOptions.Single().Value);
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
        model.SelectedSkillBonus = GrowthSkillBonus.ElephantRidersPath45;
        Assert.AreEqual(GrowthSkillBonus.None, model.SelectedSkillBonus);
        Assert.AreEqual("60", model.GrowthRates[0].Rate);
    }

    [TestMethod]
    public void PredictsThroughOneHundredAdvancesByDefault()
    {
        var model = new MainViewModel { GrowthSeed = "075bcd15" };
        Assert.AreEqual("100", model.MaxAdvances);
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        CollectionAssert.AreEqual(Enumerable.Range(0, 101).ToArray(), model.Results.Select(row => row.Advances).ToArray());
    }

    [TestMethod]
    public void PredictsAtEachSeedInDisplayOrder()
    {
        var model = CreateModel("50", "250", "450", "650", "850", "1050", "1250", "1450", "1650");
        CollectionAssert.AreEqual(new[] { "HP", "力", "魔力", "速さ", "技", "守備", "魔防", "幸運", "魅力" },
            model.GrowthRates.Select(stat => stat.Name).ToArray());
        model.MaxAdvances = "1";

        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(2, model.Results.Count);
        var first = model.Results[0];
        Assert.AreEqual("", first.ClassId);
        Assert.AreEqual(0, first.Advances);
        Assert.AreEqual("075BCD15", first.GrowthSeed);
        Assert.AreEqual(62, first.MinimumExamPassRate);
        CollectionAssert.AreEqual(new[] { 1, 2, 5, 7, 8, 11, 12, 15, 16 }, first.StatGains.ToArray());
        Assert.AreEqual("110D0CBC", first.NextGrowthSeed);
        var second = model.Results[1];
        Assert.AreEqual(1, second.Advances);
        Assert.AreEqual(first.NextGrowthSeed, second.GrowthSeed);
        Assert.AreEqual(69, second.MinimumExamPassRate);
        CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8, 10, 12, 15, 17 }, second.StatGains.ToArray());
        Assert.AreEqual("411249A3", second.NextGrowthSeed);
    }

    [DataTestMethod]
    [DataRow("0", 0)]
    [DataRow("2", 2)]
    [DataRow("9", 3)]
    public void ManualGuaranteeAppliesOnlyToEligibleStats(string guarantee, int expected)
    {
        var model = CreateModel("1", "2", "3");
        Assert.AreEqual("2", model.GuaranteedStatCount);
        model.GuaranteedStatCount = guarantee;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(expected, model.Results[0].StatGains.Count(gain => gain > 0));
    }

    [TestMethod]
    public void KeepsStartingStatsAtEveryAdvanceAndAcceptsZeroAsTheMaximum()
    {
        var model = CreateModel("10000");
        model.MaxAdvances = "10000";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(10001, model.Results.Count);
        Assert.AreEqual(99, model.Results[0].StatGains[0]);
        Assert.AreEqual(99, model.Results[1].StatGains[0]);
        Assert.AreEqual(10000, model.Results[^1].Advances);
        Assert.AreEqual(99, model.Results[^1].StatGains[0]);

        model.MaxAdvances = "0";
        model.GrowthRates[0].Rate = "32767";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(1, model.Results.Count);
        Assert.AreEqual(0, model.Results[0].Advances);
        Assert.AreEqual("075BCD15", model.Results[0].GrowthSeed);
        Assert.AreEqual(99, model.Results[0].StatGains[0]);
    }

    [DataTestMethod]
    [DataRow(2, 2, 2)]
    [DataRow(0, 1, 0)]
    [DataRow(3, 9, 0)]
    public void ExcludesCappedStatsAndCountsEachGrownStatOnce(int min, int max, int expectedRows)
    {
        var model = CreateModel("200", "0", "0", "100", "100");
        model.MaxAdvances = "1";
        model.GrowthRates[3].Condition = StatGrowthCondition.Capped;
        model.MinGrowthCount = min;
        model.MaxGrowthCount = max;

        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.IsTrue(model.HasCalculated);
        Assert.AreEqual(expectedRows, model.Results.Count);
        foreach (var row in model.Results)
            CollectionAssert.AreEqual(new[] { 2, 0, 0, 0, 1, 0, 0, 0, 0 }, row.StatGains.ToArray());
    }

    [TestMethod]
    public void FilteredRowsKeepTheirOriginalAdvancesAndSeeds()
    {
        var model = CreateModel("50", "200");
        model.GuaranteedStatCount = "0";
        model.MaxAdvances = "1";
        Assert.IsTrue(model.GrowthRates.All(stat => stat.Condition == StatGrowthCondition.Any));
        Assert.AreEqual(0, model.MinGrowthCount);
        Assert.AreEqual(9, model.MaxGrowthCount);
        Assert.IsFalse(model.HasCalculated);

        model.GrowthRates[0].Condition = StatGrowthCondition.MustNotGrow;
        model.GrowthRates[1].Condition = StatGrowthCondition.MustGrow;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        var row = model.Results.Single();
        Assert.AreEqual(1, row.Advances);
        Assert.AreEqual("110D0CBC", row.GrowthSeed);
        Assert.AreEqual("411249A3", row.NextGrowthSeed);
        CollectionAssert.AreEqual(new[] { 0, 2, 0, 0, 0, 0, 0, 0, 0 }, row.StatGains.ToArray());

        model.GrowthRates[2].Condition = StatGrowthCondition.MustGrow;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(0, model.Results.Count);
        Assert.IsTrue(model.HasCalculated);
    }

    [DataTestMethod]
    [DataRow(-1, 9)]
    [DataRow(0, 10)]
    [DataRow(5, 4)]
    public void KeepsPreviousResultsWhenGrowthCountRangeIsInvalid(int min, int max)
    {
        var model = CreateModel("100");
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        var previous = model.Results;
        model.MinGrowthCount = min;
        model.MaxGrowthCount = max;

        Assert.IsFalse(model.Calculate());
        Assert.IsFalse(string.IsNullOrWhiteSpace(model.ErrorMessage));
        Assert.AreSame(previous, model.Results);
    }

    [DataTestMethod]
    [DataRow("1234567", "1", "50")]
    [DataRow("123456789", "1", "50")]
    [DataRow("075bcg15", "1", "50")]
    [DataRow("075bcd15", "-1", "50")]
    [DataRow("075bcd15", "10001", "50")]
    [DataRow("075bcd15", "1.5", "50")]
    [DataRow("075bcd15", "1", "-1")]
    [DataRow("075bcd15", "1", "32768")]
    [DataRow("075bcd15", "1", "")]
    public void KeepsPreviousResultsWhenInputIsInvalid(string seed, string count, string rate)
    {
        var model = CreateModel("100");
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        model.GrowthSeed = seed;
        model.MaxAdvances = count;
        model.GrowthRates[8].Rate = rate;

        Assert.IsFalse(model.Calculate());
        Assert.IsFalse(string.IsNullOrWhiteSpace(model.ErrorMessage));
        Assert.AreEqual(1, model.Results.Count);
        Assert.AreEqual("075BCD15", model.Results[0].GrowthSeed);
        Assert.AreEqual("110D0CBC", model.Results[0].NextGrowthSeed);
        CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 }, model.Results[0].StatGains.ToArray());
    }

    [TestMethod]
    public void ExportsTheFilteredTableWithItsHeadersAndFormattedCells()
    {
        var model = CreateModel("50", "200");
        model.GuaranteedStatCount = "0";
        model.MaxAdvances = "1";
        model.GrowthRates[0].Condition = StatGrowthCondition.MustNotGrow;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);

        const string header = "消費数,兵種,成長seed,試験,HP,力,魔力,速さ,技,守備,魔防,幸運,魅力,次の成長seed";
        const string row = "1,,110D0CBC,69%,0,+2,0,0,0,0,0,0,0,411249A3";
        Assert.AreEqual(header + "\r\n" + row, model.CreatePredictionCsv().TrimEnd('\r', '\n'));
        Assert.AreEqual("指定無し_075BCD15_指定無し.csv", model.PredictionCsvFileName);

        model.GrowthRates[2].Condition = StatGrowthCondition.MustGrow;
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(header, model.CreatePredictionCsv().TrimEnd('\r', '\n'));
    }

    private static MainViewModel CreateModel(params string[] rates)
    {
        var model = new MainViewModel { GrowthSeed = "075bcd15", MaxAdvances = "0" };
        for (int i = 0; i < 9; i++)
            model.GrowthRates[i].Rate = i < rates.Length ? rates[i] : "0";
        return model;
    }
}
