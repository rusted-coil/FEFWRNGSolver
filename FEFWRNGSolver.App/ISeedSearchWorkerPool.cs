namespace FEFWRNGSolver.App;

public interface ISeedSearchWorkerPool : IAsyncDisposable
{
    int WorkerCount { get; }

    Task<SeedSearchRangeResult> ScanAsync(int workerIndex, long startInclusive, long endExclusive, int sampleLimit,
        CancellationToken cancellationToken);
}
