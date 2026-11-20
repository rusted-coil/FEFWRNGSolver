using FEFWRNGSolver.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FEFWRNGSolver.Presentation.Tests;

[TestClass]
public sealed class SeedSearchViewModelTests
{
    [TestMethod]
    public async Task CheckedCellsMatchThreePredictedLevelsInDisplayOrder()
    {
        var model = new SeedSearchViewModel();
        var prediction = model.Settings;
        int[] rates = [50, 120, 85, 67, 31, 44, 81, 53, 66];
        for (int i = 0; i < rates.Length; i++)
            prediction.GrowthRates[i].Rate = rates[i].ToString();
        prediction.GrowthSeed = "00000000";
        prediction.MaxAdvances = "2";
        Assert.IsTrue(prediction.Calculate(), prediction.ErrorMessage);
        Assert.AreEqual((1, 2), model.GetGainOptions()[1]);

        for (int level = 0; level < prediction.Results.Count; level++)
        {
            if (level > 0)
                model.AddObservation();
            for (int i = 0; i < rates.Length; i++)
                model.Observations[level][i] = prediction.Results[level].StatGains[i] > rates[i] / 100;
        }

        model.Changed += () =>
        {
            if (model.Scanned > 0 && model.IsRunning)
                model.Cancel();
        };
        await model.SearchAsync(work => Task.Run(work));
        Assert.IsTrue(model.WasCancelled);
        Assert.IsFalse(model.HasCompleted);
        Assert.IsTrue(model.Scanned > 0 && model.Scanned < SeedSearchViewModel.CandidateCount);
        Assert.AreEqual(0u, model.Matches[0].InitialSeed);
        Assert.AreEqual(prediction.Results[^1].NextGrowthSeed, model.Matches[0].NextSeed.ToString("X8"));
    }

    [TestMethod]
    public async Task FixedGainsMatchWholeRangeAndEditsClearResults()
    {
        var model = new SeedSearchViewModel();
        model.Settings.GrowthRates[0].Rate = "100";
        Assert.AreEqual((1, 1), model.GetGainOptions()[0]);
        await model.SearchAsync();
        Assert.IsTrue(model.HasCompleted);
        Assert.AreEqual(1L << 31, model.Scanned);
        Assert.AreEqual(model.Scanned, model.MatchCount);
        Assert.AreEqual(100, model.Matches.Count);

        model.AddObservation();
        Assert.AreEqual(2, model.Observations.Count);
        Assert.IsFalse(model.HasCompleted);
        Assert.AreEqual(0, model.Matches.Count);
    }

    [TestMethod]
    public async Task ValidatesEmptyObservationsAndEditedGrowthRates()
    {
        var model = new SeedSearchViewModel();
        model.RemoveObservation(0);
        await model.SearchAsync();
        Assert.IsNotNull(model.ErrorMessage);
        model.AddObservation();
        model.Settings.GrowthRates[0].Rate = "100";
        Assert.AreEqual((1, 1), model.GetGainOptions()[0]);
        model.Settings.Setup.UnitId = "PID_ムウ";
        model.Settings.GrowthRates[0].Rate = "200";
        Assert.AreEqual((2, 2), model.GetGainOptions()[0]);
        model.Settings.GrowthRates[0].Rate = "32768";
        await model.SearchAsync();
        Assert.IsNotNull(model.ErrorMessage);
        Assert.AreEqual(0L, model.Scanned);
    }

    [TestMethod]
    public async Task FailedWorkerBatchResumesFromConfirmedPrefix()
    {
        var model = new SeedSearchViewModel();
        model.Settings.GuaranteedStatCount = "2";
        for (int i = 0; i < 3; i++)
            model.Settings.GrowthRates[i].Rate = "50";
        model.Observations[0][0] = model.Observations[0][1] = true;

        FailingWorkerPool? workers = null;
        byte? workerGuarantee = null;
        long resumeAt = 0;
        model.Changed += () =>
        {
            if (model.IsRunning && model.WorkerCount == 1)
            {
                if (resumeAt == 0)
                    resumeAt = model.Scanned;
                else if (model.Scanned > resumeAt)
                    model.Cancel();
            }
        };
        await model.SearchWithWorkersAsync((rates, rows, targetStatCount, _) =>
        {
            workerGuarantee = targetStatCount;
            workers = new FailingWorkerPool(new SeedSearch([.. rates], rows, targetStatCount));
            return Task.FromResult<ISeedSearchWorkerPool>(workers);
        });

        Assert.AreEqual((byte)2, workerGuarantee);
        Assert.IsTrue(workers!.Disposed);
        Assert.IsTrue(model.WasCancelled);
        Assert.AreEqual(workers.FirstBatchEnd, resumeAt);
        Assert.IsTrue(resumeAt > 0 && model.Scanned > resumeAt);
        var reference = workers.Search.EvaluateRange(0, model.Scanned, SeedSearch.MaxDisplayedMatches, default);
        Assert.AreEqual(reference.MatchCount, model.MatchCount);
        CollectionAssert.AreEqual(reference.Matches, model.Matches.ToArray());
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("-1")]
    [DataRow("1.5")]
    [DataRow("10")]
    public async Task InvalidGuaranteeStopsPredictionAndSearch(string input)
    {
        var model = new SeedSearchViewModel();
        model.Settings.GrowthSeed = "00000000";
        model.Settings.GuaranteedStatCount = input;
        Assert.IsFalse(model.Settings.Calculate());
        await model.SearchAsync();
        Assert.AreEqual("最低保証は0～9の整数で入力してください。", model.ErrorMessage);
        Assert.AreEqual(model.ErrorMessage, model.Settings.ErrorMessage);
        Assert.AreEqual(0L, model.Scanned);
    }

    private sealed class FailingWorkerPool(SeedSearch search) : ISeedSearchWorkerPool
    {
        public SeedSearch Search { get; } = search;
        public int WorkerCount => 2;
        public long FirstBatchEnd { get; private set; }
        public bool Disposed { get; private set; }

        public Task<SeedSearchRangeResult> ScanAsync(int workerIndex, long startInclusive, long endExclusive,
            int sampleLimit, CancellationToken cancellationToken)
        {
            if (workerIndex == 1)
            {
                if (FirstBatchEnd > 0)
                    return Task.FromException<SeedSearchRangeResult>(new IOException("Worker disconnected"));
                FirstBatchEnd = endExclusive;
            }
            return Task.FromResult(Search.EvaluateRange(startInclusive, endExclusive, sampleLimit, cancellationToken));
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
