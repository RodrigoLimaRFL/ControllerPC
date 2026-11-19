namespace ControllerPC.Core.Profiles;

/// <summary>Switches to a specific Profile; acts as nothing if that Profile no longer exists.</summary>
public sealed record SwitchToProfileAction(Guid ProfileId) : ProfileSwitchAction;
