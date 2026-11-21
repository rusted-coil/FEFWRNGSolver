using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class MapRandomViewModelTests
{
    private static Task<LocalizationCatalog> LoadText() =>
        LocalizationCatalog.LoadAsync(path => Task.FromResult<string?>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "languages", path))), "jp", []);

    [TestMethod]
    public async Task ListsSavedMapStatesAtTheirAdvanceCounts()
    {
        var model = new MapRandomViewModel(await LoadText());
        CollectionAssert.AreEqual(new[] { "", "", "", "" }, model.Words);
        Assert.AreEqual("100", model.MaxAdvances);
        Assert.AreEqual(0, model.Results.Count);
        model.Words[0] = " 0x9c841526 ";
        model.Words[1] = "0X6B143AD2";
        model.Words[2] = "2b9f020d";
        model.Words[3] = "2D883741";
        model.MaxAdvances = "1086";

        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        // レダ編第2部の開始時、第1章、第2章のセーブにある4語。
        Assert.AreEqual(new MapRandomRow(0, "9C841526", "6B143AD2", "2B9F020D", "2D883741"), model.Results[0]);
        Assert.AreEqual(new MapRandomRow(1, "6B143AD2", "2B9F020D", "2D883741", "91193AF3"), model.Results[1]);
        Assert.AreEqual(new MapRandomRow(1086, "3996122A", "F8E5CC02", "B4ED2769", "A9BC4E27"), model.Results[^1]);
        CollectionAssert.AreEqual(Enumerable.Range(0, 1087).ToArray(), model.Results.Select(row => row.Advances).ToArray());
    }

    [TestMethod]
    public async Task IncludesBothEndsAndPadsShortWords()
    {
        var model = new MapRandomViewModel(await LoadText()) { MaxAdvances = "10000" };
        Array.Fill(model.Words, "f");
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(10001, model.Results.Count);
        Assert.AreEqual(10000, model.Results[^1].Advances);

        model.MaxAdvances = "0";
        Assert.IsTrue(model.Calculate(), model.ErrorMessage);
        Assert.AreEqual(new MapRandomRow(0, "0000000F", "0000000F", "0000000F", "0000000F"), model.Results.Single());
    }

    [DataTestMethod]
    [DataRow(0, "")]
    [DataRow(1, "123456789")]
    [DataRow(2, "0xno")]
    [DataRow(3, "0x")]
    public async Task IdentifiesInvalidWordAndKeepsPreviousResults(int index, string value)
    {
        var text = await LoadText();
        var model = new MapRandomViewModel(text) { MaxAdvances = "0" };
        Array.Fill(model.Words, "FFFFFFFF");
        Assert.IsTrue(model.Calculate());
        var previous = model.Results;

        model.Words[index] = value;
        Assert.IsFalse(model.Calculate());
        Assert.AreEqual(text.Format("SYS_INVALID_MAP_RNG_WORD", "XYZW"[index]), model.ErrorMessage);
        Assert.AreSame(previous, model.Results);
        model.Words[index] = "0";
        Assert.IsTrue(model.Calculate());
        Assert.IsNull(model.ErrorMessage);
    }

    [DataTestMethod]
    [DataRow("-1")]
    [DataRow("10001")]
    [DataRow("1.5")]
    public async Task RejectsInvalidMaximum(string maximum)
    {
        var text = await LoadText();
        var model = new MapRandomViewModel(text) { MaxAdvances = maximum };
        Array.Fill(model.Words, "0");
        Assert.IsFalse(model.Calculate());
        Assert.AreEqual(text["SYS_INVALID_MAX_RNG_ADVANCES"], model.ErrorMessage);
        Assert.AreEqual(0, model.Results.Count);
    }
}
