namespace FEFWRNGSolver.App;

public sealed class LocalSeedSearchWorkerPool : ISeedSearchWorkerPool
{
    private readonly SeedSearch m_Search;
    private readonly Func<Action, Task>? m_Run;

    public LocalSeedSearchWorkerPool(SeedSearch search, Func<Action, Task>? run, int workerCount)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);
        m_Search = search;
        m_Run = run;
        WorkerCount = workerCount;
    }

    public int WorkerCount { get; }

    public async Task<SeedSearchRangeResult> ScanAsync(int workerIndex, long startInclusive, long endExclusive,
        int sampleLimit, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(workerIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(workerIndex, WorkerCount);
        cancellationToken.ThrowIfCancellationRequested();
        if (m_Run is null)
            return m_Search.EvaluateRange(startInclusive, endExclusive, sampleLimit, cancellationToken);

        SeedSearchRangeResult? result = null;
        await m_Run(() => result = m_Search.EvaluateRange(startInclusive, endExclusive, sampleLimit, cancellationToken))
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("探索処理が実行されませんでした。");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
