using ControllerPC.Core.Output;

namespace ControllerPC.App.Tests.Fakes;

/// <summary>Ignores mouse and keyboard output.</summary>
internal sealed class SilentInputSink : IInputSink
{
    public void MoveCursor(int dx, int dy)
    {
    }

    public void Scroll(int vertical, int horizontal)
    {
    }

    public void PressMouseButton(MouseButton button)
    {
    }

    public void ReleaseMouseButton(MouseButton button)
    {
    }

    public void PressKeys(KeyCombination keys)
    {
    }

    public void ReleaseKeys(KeyCombination keys)
    {
    }
}
