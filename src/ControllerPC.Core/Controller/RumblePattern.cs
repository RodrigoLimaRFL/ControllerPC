namespace ControllerPC.Core.Controller;

/// <summary>Rumble feedback played on a controller when the mode or Active Profile changes.</summary>
public enum RumblePattern
{
    /// <summary>One short pulse.</summary>
    EnteredDesktopMode,

    /// <summary>Two short pulses.</summary>
    ExitedDesktopMode,

    /// <summary>One long pulse.</summary>
    ProfileSwitched,
}
