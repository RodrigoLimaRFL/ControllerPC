using System.Globalization;
using System.Text;

namespace ControllerPC.Core.Settings;

/// <summary>Loads and saves <see cref="AppSettings"/> as human-readable JSON.</summary>
public sealed class SettingsStore(string filePath)
{
    private const string BackupExtension = ".bad";
    private const string TemporaryExtension = ".tmp";
    private const string BackupTimestampFormat = "yyyyMMdd-HHmmss";

    /// <summary>Default location: <c>%AppData%\ControllerPC\settings.json</c>.</summary>
    public static string DefaultFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControllerPC", "settings.json");

    public string FilePath { get; } = filePath;

    /// <summary>
    /// Loads settings. Returns <see cref="DefaultSettings.Create"/> when the file does not exist. When the file
    /// cannot be parsed or has validation errors, it is moved aside to a timestamped <c>.bad</c> backup and
    /// defaults are returned, with the backup path reported in the result. A file whose <c>Version</c> is newer than
    /// <see cref="AppSettings.CurrentVersion"/> is treated the same way. Never throws for file content.
    /// </summary>
    public SettingsLoadResult Load()
    {
        if (!File.Exists(FilePath))
        {
            return new SettingsLoadResult(DefaultSettings.Create(), null);
        }

        var settings = TryParse(File.ReadAllText(FilePath, Encoding.UTF8));
        if (settings is not null && IsUsable(settings))
        {
            return new SettingsLoadResult(settings, null);
        }

        return new SettingsLoadResult(DefaultSettings.Create(), MoveToBackup());
    }

    /// <summary>Saves settings atomically (write to a temporary file, then replace), creating the directory if needed.</summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="SettingsValidator"/> reports Errors for <paramref name="settings"/>; nothing is written.
    /// </exception>
    public void Save(AppSettings settings)
    {
        RequireNoValidationErrors(settings);

        var fullPath = Path.GetFullPath(FilePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException($"\"{fullPath}\" is not a file path.");
        Directory.CreateDirectory(directory);

        var temporaryPath = fullPath + TemporaryExtension;
        try
        {
            WriteDurably(temporaryPath, SettingsSerializer.Serialize(settings));
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            DeleteIfPossible(temporaryPath);
            throw;
        }
    }

    private static void RequireNoValidationErrors(AppSettings settings)
    {
        var errors = ValidationErrors(settings);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Settings with errors cannot be saved: {string.Join(" ", errors)}");
        }
    }

    // Cleanup after a failed save must never hide the exception that made it fail.
    private static void DeleteIfPossible(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The original failure is what the caller needs to see; a leftover temporary file is harmless.
        }
    }

    private static AppSettings? TryParse(string json)
    {
        try
        {
            return SettingsSerializer.Deserialize(json);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool IsUsable(AppSettings settings) =>
        settings.Version <= AppSettings.CurrentVersion
        && ValidationErrors(settings).Count == 0;

    private static List<string> ValidationErrors(AppSettings settings) =>
        SettingsValidator.Validate(settings)
            .Where(issue => issue.Severity == IssueSeverity.Error)
            .Select(issue => issue.Message)
            .ToList();

    // Flushed to disk before the rename, so a crash cannot leave the replaced file empty.
    private static void WriteDurably(string path, string contents)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            writer.Write(contents);
        }

        stream.Flush(flushToDisk: true);
    }

    private string MoveToBackup()
    {
        var backupPath = AvailableBackupPath();
        File.Move(FilePath, backupPath);
        return backupPath;
    }

    private string AvailableBackupPath()
    {
        var timestamp = DateTime.Now.ToString(BackupTimestampFormat, CultureInfo.InvariantCulture);
        var basePath = $"{FilePath}.{timestamp}";
        var candidate = basePath + BackupExtension;

        // Two bad loads within the same second must not overwrite the first backup.
        for (var attempt = 2; File.Exists(candidate); attempt++)
        {
            candidate = $"{basePath}-{attempt}{BackupExtension}";
        }

        return candidate;
    }
}
