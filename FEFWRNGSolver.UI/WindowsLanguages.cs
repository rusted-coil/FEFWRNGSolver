using System.Globalization;
using FEFWRNGSolver.Presentation;

namespace FEFWRNGSolver.UI;

internal static class WindowsLanguages
{
    private static readonly string m_SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FEFWRNGSolver", "language.txt");

    public static LocalizationCatalog Load()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "languages");
        return LocalizationCatalog.LoadAsync(
            path => Task.FromResult(ReadFile(Path.Combine(directory, path))),
            ReadFile(m_SettingsPath)?.Trim(), [CultureInfo.CurrentUICulture.Name]).GetAwaiter().GetResult();
    }

    private static string? ReadFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(string code)
    {
        string directory = Path.GetDirectoryName(m_SettingsPath)!;
        Directory.CreateDirectory(directory);
        string temp = Path.Combine(directory, Path.GetRandomFileName());
        try
        {
            File.WriteAllText(temp, code);
            File.Move(temp, m_SettingsPath, overwrite: true);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
