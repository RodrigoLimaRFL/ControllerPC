using System.Text.Json;
using System.Text.Json.Serialization;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

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

    /// <summary>Parses JSON produced by <see cref="Serialize"/>. Throws <see cref="FormatException"/> on invalid input.</summary>
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

        var parsed = Require(settings, "all settings");
        RequireNoMissingValues(parsed);
        return parsed;
    }

    /// <summary>A deep copy, used so the settings window can edit without touching the live settings.</summary>
    public static AppSettings Clone(AppSettings settings) => Deserialize(Serialize(settings));

    // JSON can contain explicit nulls where the model's nullable annotations promise values; reject them here so
    // the rest of Core can trust those annotations.
    private static void RequireNoMissingValues(AppSettings settings)
    {
        RequireSteps(settings.BaseEntryTrigger, "the Base Entry Trigger");
        RequireSteps(settings.BaseExitTrigger, "the Base Exit Trigger");

        foreach (var profile in Require(settings.Profiles, "the Profiles"))
        {
            RequireProfileValues(Require(profile, "a Profile"));
        }
    }

    private static void RequireProfileValues(Profile profile)
    {
        Require(profile.Name, "a Profile's name");
        Require(profile.Tuning, "a Profile's movement tuning");

        foreach (var action in Require(profile.Bindings, "a Profile's Bindings").Values)
        {
            if (Require(action, "a Binding's Action") is KeyCombinationAction keyCombinationAction)
            {
                Require(keyCombinationAction.Keys, "a Key Combination");
            }
        }

        if (profile.EntryTriggerOverride is not null)
        {
            RequireSteps(profile.EntryTriggerOverride, "an Entry Trigger Override");
        }

        if (profile.ExitTriggerOverride is not null)
        {
            RequireSteps(profile.ExitTriggerOverride, "an Exit Trigger Override");
        }
    }

    private static void RequireSteps(ModeTrigger? trigger, string description) =>
        Require(Require(trigger, description).Steps, $"the Steps of {description}");

    private static T Require<T>(T? value, string description)
        where T : class =>
        value ?? throw new FormatException($"The settings are missing {description}.");
}
