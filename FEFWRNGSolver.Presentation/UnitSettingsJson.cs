using System.Text.Json;
using System.Text.Json.Serialization;

namespace FEFWRNGSolver.Presentation;

public static class UnitSettingsJson
{
    public static string Serialize(UnitSettings settings) =>
        JsonSerializer.Serialize(settings, UnitSettingsJsonContext.Default.UnitSettings);

    public static UnitSettings? Deserialize(string json) =>
        JsonSerializer.Deserialize(json, UnitSettingsJsonContext.Default.UnitSettings);
}

[JsonSerializable(typeof(UnitSettings))]
internal sealed partial class UnitSettingsJsonContext : JsonSerializerContext;
