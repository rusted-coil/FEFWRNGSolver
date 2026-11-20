using System.Text;
using System.Text.Json;
using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal sealed class FileUnitSettingsStore(Action<string, string> reportError) : IUnitSettingsStore
{
    private readonly string m_Directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FEFWRNGSolver", "Units");

    public UnitSettings? Load(string unitId)
    {
        try
        {
            return UnitSettingsJson.Deserialize(File.ReadAllText(GetPath(unitId)));
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            reportError("SYS_UNIT_SETTINGS_LOAD_ERROR", GetPath(unitId));
            return null;
        }
    }

    public void Save(string unitId, UnitSettings settings)
    {
        try
        {
            Directory.CreateDirectory(m_Directory);
            string temp = Path.Combine(m_Directory, Path.GetRandomFileName());
            try
            {
                File.WriteAllText(temp, UnitSettingsJson.Serialize(settings));
                File.Move(temp, GetPath(unitId), overwrite: true);
            }
            finally
            {
                File.Delete(temp);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            reportError("SYS_UNIT_SETTINGS_SAVE_ERROR", GetPath(unitId));
        }
    }

    private string GetPath(string unitId) =>
        Path.Combine(m_Directory, Convert.ToHexString(Encoding.UTF8.GetBytes(unitId)) + ".json");
}
