namespace ControllerPC.Core.Controller;

/// <summary>A snapshot of one controller's inputs at a point in time.</summary>
/// <param name="Buttons">
/// All digital buttons currently held, including LT/RT once pressed past the trigger threshold.
/// Only known <see cref="ControllerButton"/> flags are ever set.
/// </param>
/// <param name="LeftStick">Left analog stick.</param>
/// <param name="RightStick">Right analog stick.</param>
public readonly record struct ControllerState(
    ControllerButton Buttons,
    StickPosition LeftStick,
    StickPosition RightStick);
