using System.Diagnostics;
using FEFWRNGSolver.App;

namespace FEFWRNGSolver.Presentation;

public sealed class SeedSearchViewModel
{
    public const long CandidateCount = SeedSearch.CandidateCount;
    private readonly List<bool[]> m_Observations = [new bool[9]];
    private readonly LocalizationCatalog m_Text;
    private ValidationError? m_Error;
    private CancellationTokenSource? m_Cancellation;

    public SeedSearchViewModel() : this(new MemoryUnitSettingsStore()) { }
    public SeedSearchViewModel(IUnitSettingsStore store) : this(store, LocalizationCatalog.Default) { }
    public SeedSearchViewModel(IUnitSettingsStore store, LocalizationCatalog text)
    {
        Settings = new(store, text);
        m_Text = text;
    }

    public MainViewModel Settings { get; }
    public IReadOnlyList<bool[]> Observations => m_Observations;
    public bool IsRunning => m_Cancellation is not null;
    public bool HasCompleted { get; private set; }
    public bool WasCancelled { get; private set; }
    public int WorkerCount { get; private set; }
    public long Scanned { get; private set; }
    public long MatchCount { get; private set; }
    public IReadOnlyList<SeedSearchMatch> Matches { get; private set; } = [];
    public string? ErrorMessage => m_Error?.Translate(m_Text);
    public event Action? Changed;

    public void AddObservation()
    {
        if (IsRunning)
            return;
        m_Observations.Add(new bool[Settings.GrowthRates.Count]);
        InputsChanged();
    }

    public void RemoveObservation(int index)
    {
        if (IsRunning)
            return;
        m_Observations.RemoveAt(index);
        InputsChanged();
    }

    public void InputsChanged()
    {
        if (IsRunning)
            return;
        ResetResults();
        Changed?.Invoke();
    }

    public (int Low, int High)[] GetGainOptions()
    {
        bool valid = Settings.TryGetEffectiveRates(out var rates, out _);
        return Settings.GrowthRates.Select(stat => valid ? GainOptions(rates[stat.InternalIndex]) : (0, 1)).ToArray();
    }

    private static (int Low, int High) GainOptions(short rate) => (rate / 100, (rate + 99) / 100);

    public void Cancel() => m_Cancellation?.Cancel();

    public Task SearchAsync(Func<Action, Task>? runSlice = null) => SearchCoreAsync(runSlice, null);

    public Task SearchWithWorkersAsync(Func<short[], int[][], byte, CancellationToken, Task<ISeedSearchWorkerPool>> createWorkers)
    {
        ArgumentNullException.ThrowIfNull(createWorkers);
        return SearchCoreAsync(null, createWorkers);
    }

    private async Task SearchCoreAsync(Func<Action, Task>? runSlice,
        Func<short[], int[][], byte, CancellationToken, Task<ISeedSearchWorkerPool>>? createWorkers)
    {
        if (IsRunning)
            return;
        ResetResults();
        if (!Settings.TryGetGrowthSettings(out var rates, out var targetStatCount, out var error) || Observations.Count == 0)
        {
            m_Error = Observations.Count == 0 ? new("SYS_NO_OBSERVATIONS") : error;
            Changed?.Invoke();
            return;
        }

        // 観測表は画面の並び。抽選と照合する前に実際の増分を内部番号順に並べる。
        var gains = new int[Observations.Count][];
        for (int i = 0; i < Observations.Count; i++)
        {
            var row = Observations[i];
            var level = new int[row.Length];
            for (int column = 0; column < row.Length; column++)
            {
                int index = Settings.GrowthRates[column].InternalIndex;
                var (low, high) = GainOptions(rates[index]);
                level[index] = row[column] ? high : low;
            }
            gains[i] = level;
        }

        var search = new SeedSearch(rates, gains, targetStatCount);
        using var cancellation = new CancellationTokenSource();
        m_Cancellation = cancellation;
        var token = cancellation.Token;
        try
        {
            Changed?.Invoke();
            await Task.Delay(1, token);
            if (search.CanSkipEnumeration)
            {
                var result = search.EvaluateRange(0, CandidateCount, SeedSearch.MaxDisplayedMatches, token);
                MatchCount = result.MatchCount;
                Matches = result.Matches;
                Scanned = CandidateCount;
            }
            else if (createWorkers is not null)
            {
                try
                {
                    await using var workers = await createWorkers(rates.ToArray(), gains, targetStatCount, token);
                    await ScanAsync(workers, onUiThread: false, token);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    // Workerを起動・継続できない環境では、確定した区間の次から画面側で再開する。
                    token.ThrowIfCancellationRequested();
                }
            }

            if (Scanned < CandidateCount)
            {
                int count = runSlice is null ? 1 : Environment.ProcessorCount;
                if (count > 2)
                    count--;
                await using var workers = new LocalSeedSearchWorkerPool(search, runSlice, count);
                await ScanAsync(workers, onUiThread: runSlice is null, token);
            }
            HasCompleted = true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            WasCancelled = true;
        }
        finally
        {
            m_Cancellation = null;
            Changed?.Invoke();
        }
    }

    private async Task ScanAsync(ISeedSearchWorkerPool workers, bool onUiThread, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workers.WorkerCount, 1);
        WorkerCount = workers.WorkerCount;
        Changed?.Invoke();
        // UIスレッドで実行する場合は、短い区間で描画と中止操作へ制御を戻す。
        int rangeSize = onUiThread ? 4096 : 131072;
        while (Scanned < CandidateCount)
        {
            if (onUiThread)
                await Task.Delay(1, token);
            long started = Stopwatch.GetTimestamp();
            do
            {
                token.ThrowIfCancellationRequested();
                int count = (int)Math.Min(WorkerCount, (CandidateCount - Scanned + rangeSize - 1) / rangeSize);
                var jobs = new Task<SeedSearchRangeResult>[count];
                long end = Scanned;
                int room = SeedSearch.MaxDisplayedMatches - Matches.Count;
                for (int i = 0; i < count; i++)
                {
                    long start = end;
                    end = Math.Min(start + rangeSize, CandidateCount);
                    jobs[i] = workers.ScanAsync(i, start, end, room, token);
                }

                // WhenAllは入力順で返る。全区間が揃ってから開始seed順に確定し、途中失敗や中止で二重計上しない。
                var ranges = await Task.WhenAll(jobs);
                if (room > 0 && ranges.Any(range => range.Matches.Length > 0))
                    Matches = [.. Matches, .. ranges.SelectMany(range => range.Matches).Take(room)];
                MatchCount += ranges.Sum(range => range.MatchCount);
                Scanned = end;
            }
            while (Scanned < CandidateCount && Stopwatch.GetElapsedTime(started).TotalMilliseconds < 40);

            if (Scanned < CandidateCount)
                Changed?.Invoke();
        }
    }

    private void ResetResults()
    {
        HasCompleted = false;
        WasCancelled = false;
        WorkerCount = 0;
        Scanned = 0;
        MatchCount = 0;
        Matches = [];
        m_Error = null;
    }
}
