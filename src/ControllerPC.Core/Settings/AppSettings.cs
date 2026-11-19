using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Settings;

/// <summary>Everything the user configures: the Base Triggers and the Profiles.</summary>
public sealed class AppSettings
{
    public ModeTrigger BaseEntryTrigger { get; set; } = new();

    public ModeTrigger BaseExitTrigger { get; set; } = new();

    /// <summary>All Profiles, in display order. Always contains at least one.</summary>
    public List<Profile> Profiles { get; set; } = [];

    public Guid ActiveProfileId { get; set; }

    /// <summary>The Active Profile; falls back to the first Profile if the id is unknown.</summary>
    public Profile ActiveProfile =>
        Profiles.FirstOrDefault(p => p.Id == ActiveProfileId) ?? Profiles[0];

    public Profile? FindProfile(Guid id) => Profiles.FirstOrDefault(p => p.Id == id);

    /// <summary>The Effective Entry Trigger of <paramref name="profile"/>.</summary>
    public ModeTrigger EffectiveEntryTrigger(Profile profile) =>
        profile.EntryTriggerOverride ?? BaseEntryTrigger;

    /// <summary>The Effective Exit Trigger of <paramref name="profile"/>.</summary>
    public ModeTrigger EffectiveExitTrigger(Profile profile) =>
        profile.ExitTriggerOverride ?? BaseExitTrigger;
}
