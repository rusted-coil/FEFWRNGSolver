let search;

// .NET 10 は起動時の onmessage でスレッド種別を判定するため、独立したリスナーを使う。
self.addEventListener("message", async ({ data }) => {
    try {
        if (data.runtimeUrl) {
            const { dotnet } = await import(data.runtimeUrl);
            const { getAssemblyExports } = await dotnet.create();
            const exports = await getAssemblyExports("FEFWRNGSolver.UI.Blazor.dll");
            search = exports.FEFWRNGSolver.UI.Blazor.SeedSearchWorker;
            search.Initialize(data.rates, data.observations, data.targetStatCount);
            self.postMessage({ result: true });
        } else {
            const result = search.Scan(data.start, data.end, data.sampleLimit);
            // JSExport の Float64Array を Blazor の JSON 配列として返す。
            self.postMessage({ result: Array.from(result) });
        }
    } catch (error) {
        self.postMessage({ error: String(error) });
    }
});
