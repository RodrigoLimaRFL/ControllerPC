using ControllerPC.Core.Output;

namespace ControllerPC.Windows.Output;

/// <summary>
/// Sends synthetic mouse and keyboard input through the Win32 SendInput API. Relative moves are subject to the
/// user's pointer-speed settings, so pixels are approximate. Extended keys (arrows, navigation keys, right-hand
/// modifiers, Win) are sent with <c>KEYEVENTF_EXTENDEDKEY</c>.
/// </summary>
public sealed class SendInputSink : IInputSink
{
    public void MoveCursor(int dx, int dy) => throw new NotImplementedException();

    public void Scroll(int vertical, int horizontal) => throw new NotImplementedException();

    public void PressMouseButton(MouseButton button) => throw new NotImplementedException();

    public void ReleaseMouseButton(MouseButton button) => throw new NotImplementedException();

    public void PressKeys(KeyCombination keys) => throw new NotImplementedException();

    public void ReleaseKeys(KeyCombination keys) => throw new NotImplementedException();
}
