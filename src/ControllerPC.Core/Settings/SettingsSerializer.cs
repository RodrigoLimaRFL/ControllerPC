namespace ControllerPC.Core.Settings;

/// <summary>Converts <see cref="AppSettings"/> to and from indented, human-readable JSON.</summary>
public static class SettingsSerializer
{
    public static string Serialize(AppSettings settings) => throw new NotImplementedException();

    /// <summary>Parses JSON produced by <see cref="Serialize"/>. Throws <see cref="FormatException"/> on invalid input.</summary>
    public static AppSettings Deserialize(string json) => throw new NotImplementedException();

    /// <summary>A deep copy, used so the settings window can edit without touching the live settings.</summary>
    public static AppSettings Clone(AppSettings settings) => Deserialize(Serialize(settings));
}
