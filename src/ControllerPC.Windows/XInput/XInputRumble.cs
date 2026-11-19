using ControllerPC.Core.Controller;

namespace ControllerPC.Windows.XInput;

/// <summary>
/// Plays rumble patterns through XInputSetState without blocking the caller.
/// Disposing stops all motors, so vibration never outlives the app.
/// </summary>
public sealed class XInputRumble : IRumble, IDisposable
{
    public void Play(int slot, RumblePattern pattern) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
