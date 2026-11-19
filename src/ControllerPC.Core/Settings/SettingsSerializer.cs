using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControllerPC.Core.Settings;

/// <summary>
/// Converts <see cref="AppSettings"/> to and from indented, human-readable JSON. Enums (including the
/// <c>[Flags]</c> <see cref="Controller.ControllerButton"/> in Chords and Binding keys) are written as names.
/// </summary>
public static class SettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, Options);

    /// <summary>
    /// Parses JSON produced by <see cref="Serialize"/>. Throws <see cref="FormatException"/> on invalid input,
    /// including missing values and enum values (buttons, mouse buttons, directions, roles) that name no member.
    /// </summary>
    public static AppSettings Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        AppSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException)
        {
            throw new FormatException($"The settings are not in the expected format: {exception.Message}", exception);
        }

        var parsed = settings ?? throw new FormatException("The settings file contains no settings.");
        ParsedSettingsCheck.RequireWellFormed(parsed);
        return parsed;
    }

    /// <summary>A deep copy, used so the settings window can edit without touching the live settings.</summary>
    public static AppSettings Clone(AppSettings settings) => Deserialize(Serialize(settings));
}
