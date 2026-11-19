using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Tests.Settings;

/// <summary>Builders for the settings used across the Settings tests.</summary>
internal static class TestSettings
{
    public const ControllerButton DefaultTriggerChord =
        ControllerButton.LeftShoulder | ControllerButton.RightShoulder | ControllerButton.Start;

    /// <summary>A Mode Trigger with one Step per Chord, in order.</summary>
    public static ModeTrigger Trigger(params ControllerButton[] chords) =>
        new() { Steps = [.. chords.Select(buttons => new Chord(buttons))] };

    /// <summary>Appends a Profile with no Bindings and returns it.</summary>
    public static Profile AddProfile(AppSettings settings, string name)
    {
        var profile = new Profile { Name = name };
        settings.Profiles.Add(profile);
        return profile;
    }
}
