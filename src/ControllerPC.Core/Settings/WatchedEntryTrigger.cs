using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Settings;

/// <summary>An Entry Trigger watched outside Desktop Mode, and the Profile that becomes Active when it fires.</summary>
public sealed record WatchedEntryTrigger(ModeTrigger Trigger, Profile Profile);
