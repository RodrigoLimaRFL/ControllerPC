namespace ControllerPC.Core.Controller;

/// <summary>Position of an analog stick, each axis normalized to [-1, 1]. Positive Y is up.</summary>
public readonly record struct StickPosition(double X, double Y)
{
    public static StickPosition Center => new(0, 0);
}

/// <summary>A snapshot of one controller's inputs at a point in time.</summary>
/// <param name="Buttons">All digital buttons currently held, including the triggers past their threshold.</param>
/// <param name="LeftStick">Left analog stick.</param>
/// <param name="RightStick">Right analog stick.</param>
public readonly record struct ControllerState(
    ControllerButton Buttons,
    StickPosition LeftStick,
    StickPosition RightStick);
