namespace ControllerPC.Core.Settings;

/// <summary>Loads and saves <see cref="AppSettings"/> as human-readable JSON.</summary>
public sealed class SettingsStore(string filePath)
{
    /// <summary>Default location: <c>%AppData%\ControllerPC\settings.json</c>.</summary>
    public static string DefaultFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControllerPC", "settings.json");

    public string FilePath { get; } = filePath;

    /// <summary>Loads settings; returns <see cref="DefaultSettings.Create"/> when the file does not exist.</summary>
    public AppSettings Load() => throw new NotImplementedException();

    /// <summary>Saves settings atomically, creating the directory if needed.</summary>
    public void Save(AppSettings settings) => throw new NotImplementedException();
}
