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
        model.SelectedSkillBonus = MainViewModel.SkillBonusOptions[1].Value;

        short[] expected = [80, 70, 25, 50, 80, 80, 30, 35, 55];
        CollectionAssert.AreEqual(expected.Select(rate => rate.ToString()).ToArray(),
            model.GrowthRates.Select(stat => stat.Rate).ToArray());
        var manual = CreateModel(expected.Select(rate => rate.ToString()).ToArray());
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
        Assert.AreEqual("133", model.GrowthRates[0].Rate);
        model.SelectedSkillBonus = MainViewModel.SkillBonusOptions[0].Value;
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
    public void SelectedSkillMatchesManualRatesInDisplayOrder()
    {
        var model = CreateModel("90", "85", "95", "95", "90", "80", "95", "0", "90");
        CollectionAssert.AreEqual(new[] { "スキル補正無し", "戦車兵の道（Lv35）", "戦車兵の道（Lv45）", "戦象兵の道（Lv45）" },
            MainViewModel.SkillBonusOptions.Select(option => option.Name).ToArray());
        model.SelectedSkillBonus = MainViewModel.SkillBonusOptions[2].Value;
        CollectionAssert.AreEqual(new[] { "100", "100", "100", "100", "100", "100", "100", "5", "100" },
            model.GrowthRates.Select(stat => stat.Rate).ToArray());
        model.GrowthRates[7].Rate = "0";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1, 1, 1, 0, 1 }, model.Results[0].StatGains.ToArray());

        model.GrowthRates[0].Rate = "32767";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        model.SelectedSkillBonus = MainViewModel.SkillBonusOptions[0].Value;
        Assert.AreEqual("0", model.GrowthRates[7].Rate);
        model.SelectedSkillBonus = MainViewModel.SkillBonusOptions[2].Value;
        Assert.AreEqual("0", model.GrowthRates[7].Rate);
        model.GrowthRates[0].Rate = "32768";
        Assert.IsFalse(model.Calculate());
        Assert.IsNotNull(model.ErrorMessage);
        Assert.AreEqual(1, model.Results.Count);
    }

    [TestMethod]
    public void CalculatesConsecutiveLevelsInDisplayOrder()
    {
        var model = CreateModel("50", "250", "450", "650", "850", "1050", "1250", "1450", "1650");
        CollectionAssert.AreEqual(new[] { "HP", "力", "魔力", "速さ", "技", "守備", "魔防", "幸運", "魅力" },
            model.GrowthRates.Select(stat => stat.Name).ToArray());
        model.LevelUpCount = "2";

        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(2, model.Results.Count);
        var first = model.Results[0];
        Assert.AreEqual(1, first.Number);
        Assert.AreEqual("075BCD15", first.GrowthSeed);
        CollectionAssert.AreEqual(new[] { 1, 2, 5, 7, 8, 11, 12, 15, 16 }, first.StatGains.ToArray());
        Assert.AreEqual("110D0CBC", first.NextGrowthSeed);
        var second = model.Results[1];
        Assert.AreEqual(2, second.Number);
        Assert.AreEqual(first.NextGrowthSeed, second.GrowthSeed);
        CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8, 10, 12, 15, 17 }, second.StatGains.ToArray());
        Assert.AreEqual("411249A3", second.NextGrowthSeed);
    }

    [TestMethod]
    public void FillsTwoMissingStatsInTheGamesDrawOrder()
    {
        var model = CreateModel("0", "0", "0", "1", "2", "3", "4", "5", "0");
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 1, 1, 0, 0 }, model.Results[0].StatGains.ToArray());
    }

    [TestMethod]
    public void CarriesCappedStatsAndStartsFreshWhenRecalculated()
    {
        var model = CreateModel("10000");
        model.LevelUpCount = "10000";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(10000, model.Results.Count);
        Assert.AreEqual(99, model.Results[0].StatGains[0]);
        Assert.AreEqual(0, model.Results[1].StatGains[0]);
        Assert.AreEqual(10000, model.Results[^1].Number);
        Assert.AreEqual(0, model.Results[^1].StatGains[0]);

        model.LevelUpCount = "1";
        model.GrowthRates[0].Rate = "32767";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(1, model.Results.Count);
        Assert.AreEqual(99, model.Results[0].StatGains[0]);
    }

    [DataTestMethod]
    [DataRow("1234567", "1", "50")]
    [DataRow("123456789", "1", "50")]
    [DataRow("075bcg15", "1", "50")]
    [DataRow("075bcd15", "0", "50")]
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
        model.LevelUpCount = count;
        model.GrowthRates[8].Rate = rate;

        Assert.IsFalse(model.Calculate());
        Assert.IsFalse(string.IsNullOrWhiteSpace(model.ErrorMessage));
        Assert.AreEqual(1, model.Results.Count);
        Assert.AreEqual("075BCD15", model.Results[0].GrowthSeed);
        Assert.AreEqual("110D0CBC", model.Results[0].NextGrowthSeed);
        CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 }, model.Results[0].StatGains.ToArray());
    }

    private static MainViewModel CreateModel(params string[] rates)
    {
        var model = new MainViewModel { GrowthSeed = "075bcd15", LevelUpCount = "1" };
        for (int i = 0; i < 9; i++)
            model.GrowthRates[i].Rate = i < rates.Length ? rates[i] : "0";
        return model;
    }
}
