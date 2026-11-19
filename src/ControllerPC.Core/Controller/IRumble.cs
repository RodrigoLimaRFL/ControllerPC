namespace ControllerPC.Core.Controller;

/// <summary>Rumble feedback patterns played on a controller.</summary>
public enum RumblePattern
{
    /// <summary>One short pulse.</summary>
    EnteredDesktopMode,

    /// <summary>Two short pulses.</summary>
    ExitedDesktopMode,

    /// <summary>One long pulse.</summary>
    ProfileSwitched,
}

/// <summary>Plays rumble feedback. Implementations must not block the caller.</summary>
public interface IRumble
{
    void Play(int slot, RumblePattern pattern);
}
