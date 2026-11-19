using ControllerPC.Core.Settings;

namespace ControllerPC.App;

/// <summary>Loads the settings at start-up and tells the user when defaults had to be used instead.</summary>
internal static class StartupSettings
{
    private const string Title = "ControllerPC";

    public static AppSettings Load(SettingsStore store)
    {
        try
        {
            var result = store.Load();
            if (result.CorruptFileBackupPath is { } backupPath)
            {
                MessageBox.Show(
                    "The settings file could not be read, so the default settings were loaded.\n\n"
                    + $"The unreadable file was kept as:\n{backupPath}",
                    Title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return result.Settings;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"The settings file could not be opened:\n{exception.Message}\n\n"
                + $"ControllerPC starts with the default settings.\n\nSettings file:\n{store.FilePath}",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return DefaultSettings.Create();
        }
    }
}
