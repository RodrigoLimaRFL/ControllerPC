using ControllerPC.Core.Controller;

namespace ControllerPC.Windows.XInput;

/// <summary>Plays rumble patterns through XInputSetState without blocking the caller.</summary>
public sealed class XInputRumble : IRumble
{
    public void Play(int slot, RumblePattern pattern) => throw new NotImplementedException();
}
