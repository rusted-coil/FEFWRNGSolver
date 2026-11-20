using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using FEFWRNGSolver.App;

namespace FEFWRNGSolver.UI.Blazor;

[SupportedOSPlatform("browser")]
internal static partial class SeedSearchWorker
{
    // 各 Web Worker は独立した .NET ランタイムを持つ。
    private static SeedSearch? m_Search;

    [JSExport]
    internal static void Initialize(int[] rates, int[] observations, int targetStatCount)
    {
        m_Search = new SeedSearch([.. rates.Select(rate => checked((short)rate))],
            observations.Chunk(rates.Length).ToArray(), checked((byte)targetStatCount));
    }

    [JSExport]
    internal static double[] Scan(double startInclusive, double endExclusive, int sampleLimit)
    {
        var result = m_Search!.EvaluateRange((long)startInclusive, (long)endExclusive, sampleLimit,
            CancellationToken.None);
        // JS の Number で件数（最大 2^31）と各 seed を正確に運ぶ。先頭は件数、以降は seed の組。
        var values = new double[1 + 2 * result.Matches.Length];
        values[0] = result.MatchCount;
        for (int i = 0; i < result.Matches.Length; i++)
        {
            values[1 + 2 * i] = result.Matches[i].InitialSeed;
            values[2 + 2 * i] = result.Matches[i].NextSeed;
        }
        return values;
    }
}
