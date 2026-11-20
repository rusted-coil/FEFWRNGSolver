using System.Text.Json;
using FEFWRNGSolver.Presentation;
using Microsoft.JSInterop;

namespace FEFWRNGSolver.UI.Blazor;

internal sealed class BrowserUnitSettingsStore(IJSInProcessRuntime javaScript) : IUnitSettingsStore
{
    private const string KeyPrefix = "FEFWRNGSolver.unit.";
    private const string SaveError = "ユニット設定をブラウザに保存できませんでした。このページを閉じると変更は失われます。";
    private readonly Dictionary<string, UnitSettings> m_Unsaved = new();

    public string? ErrorMessage { get; private set; }

    public UnitSettings? Load(string unitId)
    {
        if (m_Unsaved.TryGetValue(unitId, out var settings))
        {
            ErrorMessage = SaveError;
            return settings;
        }

        try
        {
            ErrorMessage = null;
            var json = javaScript.Invoke<string?>("localStorage.getItem", KeyPrefix + unitId);
            return json is null ? null : UnitSettingsJson.Deserialize(json);
        }
        catch (Exception ex) when (ex is JSException or JsonException)
        {
            ErrorMessage = "このユニットの設定をブラウザから読み込めませんでした。";
            return null;
        }
    }

    public void Save(string unitId, UnitSettings settings)
    {
        try
        {
            javaScript.InvokeVoid("localStorage.setItem", KeyPrefix + unitId, UnitSettingsJson.Serialize(settings));
            m_Unsaved.Remove(unitId);
            ErrorMessage = null;
        }
        catch (JSException)
        {
            m_Unsaved[unitId] = settings;
            ErrorMessage = SaveError;
        }
    }
}
