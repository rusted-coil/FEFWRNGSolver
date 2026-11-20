using System.Text.Json;
using FEFWRNGSolver.Presentation;
using Microsoft.JSInterop;

namespace FEFWRNGSolver.UI.Blazor;

internal sealed class BrowserUnitSettingsStore(IJSInProcessRuntime javaScript, LocalizationCatalog text) : IUnitSettingsStore
{
    private const string KeyPrefix = "FEFWRNGSolver.unit.";
    private const string SaveError = "SYS_BROWSER_SAVE_ERROR";
    private readonly Dictionary<string, UnitSettings> m_Unsaved = new();

    private string? m_ErrorKey;
    public string? ErrorMessage => m_ErrorKey is null ? null : text[m_ErrorKey];

    public UnitSettings? Load(string unitId)
    {
        if (m_Unsaved.TryGetValue(unitId, out var settings))
        {
            m_ErrorKey = SaveError;
            return settings;
        }

        try
        {
            m_ErrorKey = null;
            var json = javaScript.Invoke<string?>("localStorage.getItem", KeyPrefix + unitId);
            return json is null ? null : UnitSettingsJson.Deserialize(json);
        }
        catch (Exception ex) when (ex is JSException or JsonException)
        {
            m_ErrorKey = "SYS_BROWSER_LOAD_ERROR";
            return null;
        }
    }

    public void Save(string unitId, UnitSettings settings)
    {
        try
        {
            javaScript.InvokeVoid("localStorage.setItem", KeyPrefix + unitId, UnitSettingsJson.Serialize(settings));
            m_Unsaved.Remove(unitId);
            m_ErrorKey = null;
        }
        catch (JSException)
        {
            m_Unsaved[unitId] = settings;
            m_ErrorKey = SaveError;
        }
    }
}
