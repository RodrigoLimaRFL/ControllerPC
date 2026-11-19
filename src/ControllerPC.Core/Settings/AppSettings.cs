using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Settings;

/// <summary>Everything the user configures: the Base Triggers and the Profiles.</summary>
public sealed class AppSettings
{
    /// <summary>Version of the settings file format written by this build.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public ModeTrigger BaseEntryTrigger { get; set; } = new();

    public ModeTrigger BaseExitTrigger { get; set; } = new();

    /// <summary>All Profiles, in display order. Valid settings always contain at least one.</summary>
    public List<Profile> Profiles { get; set; } = [];

    public Guid ActiveProfileId { get; set; }

    /// <summary>The Active Profile; falls back to the first Profile if the id is unknown.</summary>
    /// <exception cref="InvalidOperationException">There are no Profiles.</exception>
    public Profile ActiveProfile =>
        FindProfile(ActiveProfileId)
        ?? Profiles.FirstOrDefault()
        ?? throw new InvalidOperationException("Settings contain no Profiles.");

    public Profile? FindProfile(Guid id) => Profiles.FirstOrDefault(p => p.Id == id);

    /// <summary>The Effective Entry Trigger of <paramref name="profile"/>.</summary>
    public ModeTrigger EffectiveEntryTrigger(Profile profile) =>
        profile.EntryTriggerOverride ?? BaseEntryTrigger;

    /// <summary>The Effective Exit Trigger of <paramref name="profile"/>.</summary>
    public ModeTrigger EffectiveExitTrigger(Profile profile) =>
        profile.ExitTriggerOverride ?? BaseExitTrigger;

    /// <summary>
    /// The Entry Triggers watched outside Desktop Mode: every Profile's Entry Trigger Override (entering with
    /// that Profile), plus the Base Entry Trigger (entering with the Active Profile) when the Active Profile has
    /// no Entry Trigger Override.
    /// </summary>
    public IReadOnlyList<WatchedEntryTrigger> WatchedEntryTriggers()
    {
        var watched = Profiles
            .Where(p => p.EntryTriggerOverride is not null)
            .Select(p => new WatchedEntryTrigger(p.EntryTriggerOverride!, p))
            .ToList();

        var active = ActiveProfile;
        if (active.EntryTriggerOverride is null)
        {
            watched.Add(new WatchedEntryTrigger(BaseEntryTrigger, active));
        }

        return watched;
    }
}
