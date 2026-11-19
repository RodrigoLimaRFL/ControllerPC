namespace ControllerPC.Core.Output;

public enum MouseButton
{
    Left,
    Right,
    Middle,
    Back,
    Forward,
}

/// <summary>Sends synthetic mouse and keyboard input to the PC.</summary>
public interface IInputSink
{
    /// <summary>Moves the cursor relative to its current position, in pixels.</summary>
    void MoveCursor(int dx, int dy);

    /// <summary>Scrolls by wheel deltas (120 = one notch). Positive vertical scrolls up, positive horizontal scrolls right.</summary>
    void Scroll(int vertical, int horizontal);

    void PressMouseButton(MouseButton button);

    void ReleaseMouseButton(MouseButton button);

    /// <summary>Presses every key of the combination, in order.</summary>
    void PressKeys(KeyCombination keys);

    /// <summary>Releases every key of the combination, in reverse order.</summary>
    void ReleaseKeys(KeyCombination keys);
}
