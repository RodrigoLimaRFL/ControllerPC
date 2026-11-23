using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Settings;

/// <summary>
/// Rejects parsed settings that JSON allows but the model cannot represent, so the rest of Core can trust them:
/// explicit nulls where the nullable annotations promise values, and enum values that name no member.
/// </summary>
/// <remarks>
/// The enum converter accepts numbers and numeric strings (e.g. <c>99</c> or <c>"262144"</c>) for any value, and an
/// undefined button in the Base Entry Trigger could never be pressed, locking the user out of Desktop Mode.
/// </remarks>
internal static class ParsedSettingsCheck
{
    private static readonly ControllerButton KnownButtons =
        ControllerButtons.All.Aggregate(ControllerButton.None, (known, button) => known | button);

    /// <exception cref="FormatException">The settings contain a missing or undefined value.</exception>
    public static void RequireWellFormed(AppSettings settings)
    {
        RequireTrigger(settings.BaseEntryTrigger, "the Base Entry Trigger");
        RequireTrigger(settings.BaseExitTrigger, "the Base Exit Trigger");

        foreach (var profile in Require(settings.Profiles, "the Profiles"))
        {
            RequireProfile(Require(profile, "a Profile"));
        }
    }

    private static void RequireProfile(Profile profile)
    {
        Require(profile.Name, "a Profile's name");
        Require(profile.Tuning, "a Profile's movement tuning");
        RequireDefined(profile.RightStickRole, "Right Stick Role");

        foreach (var (button, action) in Require(profile.Bindings, "a Profile's Bindings"))
        {
            RequireKnownButtons(button, "a Binding");
            RequireAction(Require(action, "a Binding's Action"));
        }

        foreach (var chordBinding in Require(profile.ChordBindings, "a Profile's Chord Bindings"))
        {
            var binding = Require(chordBinding, "a Chord Binding");
            RequireKnownButtons(binding.Chord.Buttons, "a Chord Binding");
            RequireAction(Require(binding.Action, "a Chord Binding's Action"));
        }

        if (profile.EntryTriggerOverride is not null)
        {
            RequireTrigger(profile.EntryTriggerOverride, "an Entry Trigger Override");
        }

        if (profile.ExitTriggerOverride is not null)
        {
            RequireTrigger(profile.ExitTriggerOverride, "an Exit Trigger Override");
        }
    }

    private static void RequireAction(ButtonAction action)
    {
        switch (action)
        {
            case KeyCombinationAction keyCombinationAction:
                Require(keyCombinationAction.Keys, "a Key Combination");
                break;
            case MacroAction macroAction:
                foreach (var keys in macroAction.KeyCombinations)
                {
                    Require(keys, "a Key Combination of a Macro");
                }

                break;
            case MouseButtonAction mouseButtonAction:
                RequireDefined(mouseButtonAction.Button, "mouse button");
                break;
            case NudgeAction nudgeAction:
                RequireDefined(nudgeAction.Direction, "Nudge direction");
                break;
        }
    }

    private static void RequireTrigger(ModeTrigger? trigger, string description)
    {
        var steps = Require(Require(trigger, description).Steps, $"the Steps of {description}");
        foreach (var chord in steps)
        {
            RequireKnownButtons(chord.Buttons, $"a Chord of {description}");
        }
    }

    private static void RequireKnownButtons(ControllerButton buttons, string description)
    {
        if ((buttons & ~KnownButtons) != ControllerButton.None)
        {
            throw new FormatException($"The settings use an unknown controller button in {description}: {buttons}.");
        }
    }

    private static void RequireDefined<TEnum>(TEnum value, string description)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new FormatException($"The settings contain an unknown {description}: {value}.");
        }
    }

    private static T Require<T>(T? value, string description)
        where T : class =>
        value ?? throw new FormatException($"The settings are missing {description}.");
}
