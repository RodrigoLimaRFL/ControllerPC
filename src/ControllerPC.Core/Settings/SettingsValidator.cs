using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.Settings;

/// <summary>
/// Checks settings before saving.
/// <para>Errors (block saving):</para>
/// <list type="bullet">
/// <item>No Profiles; duplicate Profile ids; blank or duplicate Profile names (case-insensitive).</item>
/// <item>A Mode Trigger (Base or override) with no Steps, or with an empty Chord.</item>
/// <item>A Mode Trigger with Step Gap or Final Hold duration &lt;= 0.</item>
/// <item>Two watched Entry Trigger sources with identical Steps: a Profile's Entry Trigger Override equal to
/// the Base Entry Trigger or to another Profile's override.</item>
/// <item>A Binding whose key is <c>None</c> or more than one button, or a Key Combination with no keys (in a Binding
/// or a Chord Binding).</item>
/// <item>A Chord Binding with fewer than two buttons; two Chord Bindings with the same Chord in one Profile.</item>
/// <item>A Chord Binding whose buttons are all part of one Step's Chord of the Profile's Effective Exit Trigger: it would
/// be performed on every exit.</item>
/// <item>Tuning out of range: Deadzone outside [0, 1), CursorSpeed or ScrollSpeed &lt;= 0, NudgeStep &lt; 1,
/// Chord Wait outside [0, <see cref="Profile.MaxChordWait"/>].</item>
/// </list>
/// <para>Warnings (saving allowed):</para>
/// <list type="bullet">
/// <item>One Entry Trigger's Steps are a strict prefix of another's (the shorter one may fire first and prevent the
/// longer one).</item>
/// <item>A Profile's Effective Exit Trigger uses buttons that the same Profile binds to Key Combinations or mouse
/// buttons.</item>
/// <item>A "Switch to Profile" Action (of a Binding or a Chord Binding) pointing at a Profile that no longer exists.</item>
/// </list>
/// Chord Bindings are not checked against Entry Triggers, which are only watched outside Desktop Mode.
/// Identical Entry and Exit Triggers are allowed. Two Mode Triggers are identical when their Steps are equal;
/// timing settings are not compared.
/// </summary>
/// <remarks>
/// <para>Entry Trigger conflicts compare every Entry Trigger Override with the Base Entry Trigger and with every other
/// override, whichever Profile is active, because the Active Profile changes at run time.</para>
/// <para>The prefix Warning is given even when the shorter trigger has Final Hold: tapping its last Chord lets the
/// longer one continue, but holding it fires the shorter one, so the user should still know.</para>
/// </remarks>
public static class SettingsValidator
{
    private const int MinimumChordBindingButtons = 2;

    public static IReadOnlyList<ValidationIssue> Validate(AppSettings settings) =>
    [
        .. ProfileListIssues(settings.Profiles),
        .. AllModeTriggers(settings).SelectMany(ModeTriggerIssues),
        .. EntryTriggerConflictIssues(settings),
        .. settings.Profiles.SelectMany(BindingIssues),
        .. settings.Profiles.SelectMany(profile => ChordBindingIssues(settings, profile)),
        .. settings.Profiles.SelectMany(TuningIssues),
        .. EntryTriggerPrefixWarnings(settings),
        .. settings.Profiles.SelectMany(profile => ExitTriggerHeldInputWarnings(settings, profile)),
        .. settings.Profiles.SelectMany(profile => MissingSwitchTargetWarnings(settings, profile)),
    ];

    private static IEnumerable<ValidationIssue> ProfileListIssues(IReadOnlyList<Profile> profiles)
    {
        if (profiles.Count == 0)
        {
            yield return Error("There must be at least one Profile.");
        }

        foreach (var duplicateId in profiles.GroupBy(p => p.Id).Where(g => g.Count() > 1).Select(g => g.Key))
        {
            yield return Error("Several Profiles share the same id.", duplicateId);
        }

        foreach (var profile in profiles.Where(p => string.IsNullOrWhiteSpace(p.Name)))
        {
            yield return Error("Every Profile needs a name.", profile.Id);
        }

        var namedProfiles = profiles.Where(p => !string.IsNullOrWhiteSpace(p.Name));
        foreach (var sameName in namedProfiles.GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            foreach (var duplicate in sameName.Skip(1))
            {
                yield return Error(
                    $"More than one Profile is named \"{duplicate.Name}\". Profile names must be unique.",
                    duplicate.Id);
            }
        }
    }

    private static IEnumerable<ValidationIssue> ModeTriggerIssues(DescribedModeTrigger trigger)
    {
        if (trigger.Steps.Count == 0)
        {
            yield return Error($"The {trigger.Description} has no Steps.", trigger.ProfileId);
        }

        for (var index = 0; index < trigger.Steps.Count; index++)
        {
            if (trigger.Steps[index].IsEmpty)
            {
                yield return Error(
                    $"Step {index + 1} of the {trigger.Description} has an empty Chord.", trigger.ProfileId);
            }
        }

        if (trigger.Trigger.StepGap <= TimeSpan.Zero)
        {
            yield return Error(
                $"The Step Gap of the {trigger.Description} must be greater than zero.", trigger.ProfileId);
        }

        if (trigger.Trigger.FinalHoldDuration <= TimeSpan.Zero)
        {
            yield return Error(
                $"The Final Hold duration of the {trigger.Description} must be greater than zero.", trigger.ProfileId);
        }
    }

    private static IEnumerable<ValidationIssue> EntryTriggerConflictIssues(AppSettings settings)
    {
        // A trigger without Steps already has its own error; comparing it would only repeat that.
        var baseEntry = BaseEntryTrigger(settings);
        var overrides = EntryTriggerOverrides(settings).Where(trigger => trigger.Steps.Count > 0).ToList();

        for (var index = 0; index < overrides.Count; index++)
        {
            var current = overrides[index];
            if (current.HasSameSteps(baseEntry))
            {
                yield return Error(
                    $"The {current.Description} has the same Steps as the {baseEntry.Description}.", current.ProfileId);
            }

            foreach (var earlier in overrides.Take(index).Where(current.HasSameSteps))
            {
                yield return Error(
                    $"The {current.Description} has the same Steps as the {earlier.Description}.", current.ProfileId);
            }
        }
    }

    private static IEnumerable<ValidationIssue> BindingIssues(Profile profile)
    {
        foreach (var (button, action) in profile.Bindings)
        {
            if (!ControllerButtons.All.Contains(button))
            {
                yield return Error(
                    $"Profile \"{profile.Name}\" has a Binding for \"{button}\". A Binding must be for exactly one button.",
                    profile.Id);
            }

            if (action is KeyCombinationAction { Keys.IsEmpty: true })
            {
                yield return Error(
                    $"The Binding for {button} in Profile \"{profile.Name}\" has a Key Combination with no keys.",
                    profile.Id);
            }
        }
    }

    private static IEnumerable<ValidationIssue> ChordBindingIssues(AppSettings settings, Profile profile)
    {
        var exitChords = settings.EffectiveExitTrigger(profile).Steps.Select(step => step.Buttons).ToList();
        foreach (var chordBinding in profile.ChordBindings)
        {
            var chord = chordBinding.Chord.Buttons;
            var name = ChordName(chord);
            if (ControllerButtons.Split(chord).Count() < MinimumChordBindingButtons)
            {
                yield return Error(
                    $"The Chord Binding {name} in Profile \"{profile.Name}\" needs at least two buttons.", profile.Id);
                continue;
            }

            if (exitChords.Any(exitChord => (chord & ~exitChord) == ControllerButton.None))
            {
                yield return Error(
                    $"The Chord Binding {name} in Profile \"{profile.Name}\" only uses buttons of a Step of the Profile's " +
                    "Effective Exit Trigger, so it would be performed every time Desktop Mode is exited.",
                    profile.Id);
            }

            if (chordBinding.Action is KeyCombinationAction { Keys.IsEmpty: true })
            {
                yield return Error(
                    $"The Chord Binding {name} in Profile \"{profile.Name}\" has a Key Combination with no keys.",
                    profile.Id);
            }
        }

        var duplicateChords = profile.ChordBindings
            .GroupBy(chordBinding => chordBinding.Chord)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.Buttons);
        foreach (var chord in duplicateChords)
        {
            yield return Error(
                $"Profile \"{profile.Name}\" has more than one Chord Binding for {ChordName(chord)}.", profile.Id);
        }
    }

    /// <summary>How messages name a Chord, e.g. <c>A+B</c>.</summary>
    private static string ChordName(ControllerButton chord) =>
        chord == ControllerButton.None ? "with no buttons" : string.Join("+", ControllerButtons.Split(chord));

    // The range checks are written negated so that NaN fails them too.
    private static IEnumerable<ValidationIssue> TuningIssues(Profile profile)
    {
        var tuning = profile.Tuning;
        if (!(tuning.Deadzone >= 0 && tuning.Deadzone < 1))
        {
            yield return Error(
                $"The Deadzone of Profile \"{profile.Name}\" must be at least 0 and less than 1.", profile.Id);
        }

        if (!(tuning.CursorSpeed > 0))
        {
            yield return Error($"The cursor speed of Profile \"{profile.Name}\" must be greater than zero.", profile.Id);
        }

        if (!(tuning.ScrollSpeed > 0))
        {
            yield return Error($"The scroll speed of Profile \"{profile.Name}\" must be greater than zero.", profile.Id);
        }

        if (tuning.NudgeStep < 1)
        {
            yield return Error($"The Nudge step of Profile \"{profile.Name}\" must be at least 1 pixel.", profile.Id);
        }

        if (profile.ChordWait < TimeSpan.Zero || profile.ChordWait > Profile.MaxChordWait)
        {
            yield return Error(
                $"The Chord Wait of Profile \"{profile.Name}\" must be between 0 and " +
                $"{Profile.MaxChordWait.TotalMilliseconds} ms.",
                profile.Id);
        }
    }

    private static IEnumerable<ValidationIssue> EntryTriggerPrefixWarnings(AppSettings settings)
    {
        var entryTriggers = EntryTriggerOverrides(settings)
            .Prepend(BaseEntryTrigger(settings))
            .Where(trigger => trigger.Steps.Count > 0)
            .ToList();

        foreach (var shorter in entryTriggers)
        {
            foreach (var longer in entryTriggers.Where(shorter.IsStrictPrefixOf))
            {
                yield return Warning(
                    $"The {longer.Description} starts with all the Steps of the {shorter.Description}, so the " +
                    $"{shorter.Description} may fire first and prevent the {longer.Description} from firing.",
                    longer.ProfileId);
            }
        }
    }

    private static IEnumerable<ValidationIssue> ExitTriggerHeldInputWarnings(AppSettings settings, Profile profile)
    {
        var exitButtons = settings.EffectiveExitTrigger(profile).Steps
            .Aggregate(ControllerButton.None, (buttons, chord) => buttons | chord.Buttons);
        var overlapping = ControllerButtons.Split(exitButtons)
            .Where(button => profile.GetAction(button) is KeyCombinationAction or MouseButtonAction)
            .ToList();

        if (overlapping.Count > 0)
        {
            yield return Warning(
                $"The Effective Exit Trigger of Profile \"{profile.Name}\" uses {string.Join(", ", overlapping)}, which this " +
                "Profile binds to Key Combinations or mouse buttons. Performing the Exit Trigger will also press them.",
                profile.Id);
        }
    }

    private static IEnumerable<ValidationIssue> MissingSwitchTargetWarnings(AppSettings settings, Profile profile)
    {
        foreach (var (button, action) in profile.Bindings)
        {
            if (action is SwitchToProfileAction switchAction && settings.FindProfile(switchAction.ProfileId) is null)
            {
                yield return Warning(
                    $"The Binding for {button} in Profile \"{profile.Name}\" switches to a Profile that no longer exists.",
                    profile.Id);
            }
        }

        foreach (var chordBinding in profile.ChordBindings)
        {
            if (chordBinding.Action is SwitchToProfileAction switchAction && settings.FindProfile(switchAction.ProfileId) is null)
            {
                yield return Warning(
                    $"The Chord Binding {ChordName(chordBinding.Chord.Buttons)} in Profile \"{profile.Name}\" switches to " +
                    "a Profile that no longer exists.",
                    profile.Id);
            }
        }
    }

    private static IEnumerable<DescribedModeTrigger> AllModeTriggers(AppSettings settings)
    {
        yield return BaseEntryTrigger(settings);
        yield return new DescribedModeTrigger(settings.BaseExitTrigger, "Base Exit Trigger", null);

        foreach (var entryOverride in EntryTriggerOverrides(settings))
        {
            yield return entryOverride;
        }

        foreach (var profile in settings.Profiles)
        {
            if (profile.ExitTriggerOverride is { } exitOverride)
            {
                yield return new DescribedModeTrigger(
                    exitOverride, $"Exit Trigger Override of Profile \"{profile.Name}\"", profile.Id);
            }
        }
    }

    private static DescribedModeTrigger BaseEntryTrigger(AppSettings settings) =>
        new(settings.BaseEntryTrigger, "Base Entry Trigger", null);

    private static IEnumerable<DescribedModeTrigger> EntryTriggerOverrides(AppSettings settings)
    {
        foreach (var profile in settings.Profiles)
        {
            if (profile.EntryTriggerOverride is { } entryOverride)
            {
                yield return new DescribedModeTrigger(
                    entryOverride, $"Entry Trigger Override of Profile \"{profile.Name}\"", profile.Id);
            }
        }
    }

    private static ValidationIssue Error(string message, Guid? profileId = null) =>
        new(IssueSeverity.Error, message, profileId);

    private static ValidationIssue Warning(string message, Guid? profileId = null) =>
        new(IssueSeverity.Warning, message, profileId);
}
