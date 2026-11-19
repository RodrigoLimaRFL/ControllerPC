namespace ControllerPC.Core.Settings;

/// <summary>Loads and saves <see cref="AppSettings"/> as human-readable JSON.</summary>
public sealed class SettingsStore(string filePath)
{
    /// <summary>Default location: <c>%AppData%\ControllerPC\settings.json</c>.</summary>
    public static string DefaultFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControllerPC", "settings.json");

    public string FilePath { get; } = filePath;

    /// <summary>
    /// Loads settings. Returns <see cref="DefaultSettings.Create"/> when the file does not exist. When the file
    /// cannot be parsed or has validation errors, it is moved aside to a timestamped <c>.bad</c> backup and
    /// defaults are returned, with the backup path reported in the result. Never throws for file content.
    /// </summary>
    public SettingsLoadResult Load() => throw new NotImplementedException();

    /// <summary>Saves settings atomically (write to a temporary file, then replace), creating the directory if needed.</summary>
    public void Save(AppSettings settings) => throw new NotImplementedException();
}
