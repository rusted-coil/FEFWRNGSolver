using FEFWRNGSolver.App;
using Microsoft.JSInterop;

namespace FEFWRNGSolver.UI.Blazor;

internal sealed class BrowserSeedSearchPool(IJSObjectReference workers, int workerCount) : ISeedSearchWorkerPool
{
    private readonly IJSObjectReference m_Workers = workers;
    public int WorkerCount { get; } = workerCount;

    public static async Task<ISeedSearchWorkerPool> CreateAsync(IJSRuntime js, short[] rates, int[][] observations,
        CancellationToken cancellationToken)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", cancellationToken,
            "./_content/FEFWRNGSolver.UI.Blazor/seedWorkers.js");
        var workers = await module.InvokeAsync<IJSObjectReference>("createPool");
        try
        {
            int count = await workers.InvokeAsync<int>("start", cancellationToken, rates, observations);
            return new BrowserSeedSearchPool(workers, count);
        }
        catch
        {
            await StopAsync(workers);
            throw;
        }
    }

    public async Task<SeedSearchRangeResult> ScanAsync(int workerIndex, long startInclusive, long endExclusive,
        int sampleLimit, CancellationToken cancellationToken)
    {
        var values = await m_Workers.InvokeAsync<double[]>("scan", cancellationToken, workerIndex, startInclusive,
            endExclusive, sampleLimit);
        var matches = new SeedSearchMatch[(values.Length - 1) / 2];
        for (int i = 0; i < matches.Length; i++)
            matches[i] = new((uint)values[1 + 2 * i], (uint)values[2 + 2 * i]);
        return new SeedSearchRangeResult((long)values[0], matches);
    }

    public ValueTask DisposeAsync() => StopAsync(m_Workers);

    private static async ValueTask StopAsync(IJSObjectReference workers)
    {
        try { await workers.InvokeVoidAsync("stop"); }
        finally { await workers.DisposeAsync(); }
    }
}
