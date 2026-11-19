namespace ControllerPC.Core.Output;

/// <summary>Sends synthetic mouse and keyboard input to the PC. Implementations do no interpretation.</summary>
public interface IInputSink
{
    /// <summary>
    /// Moves the cursor relative to its current position, in screen coordinates:
    /// positive <paramref name="dx"/> moves right, positive <paramref name="dy"/> moves <b>down</b>.
    /// </summary>
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
