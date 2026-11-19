namespace ControllerPC.Core.Controller;

/// <summary>Plays rumble feedback on a controller.</summary>
public interface IRumble
{
    /// <summary>
    /// Starts <paramref name="pattern"/> on the controller in <paramref name="slot"/> and returns immediately.
    /// A new pattern on the same slot replaces the one in progress.
    /// </summary>
    void Play(int slot, RumblePattern pattern);
}
