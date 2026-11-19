namespace ControllerPC.Core.Controller;

/// <summary>Helpers for working with sets of <see cref="ControllerButton"/> flags.</summary>
public static class ControllerButtons
{
    /// <summary>Every individual digital button, in a stable display order.</summary>
    public static IReadOnlyList<ControllerButton> All { get; } =
    [
        ControllerButton.A, ControllerButton.B, ControllerButton.X, ControllerButton.Y,
        ControllerButton.LeftShoulder, ControllerButton.RightShoulder,
        ControllerButton.LeftTrigger, ControllerButton.RightTrigger,
        ControllerButton.Start, ControllerButton.Back,
        ControllerButton.LeftThumb, ControllerButton.RightThumb,
        ControllerButton.DPadUp, ControllerButton.DPadDown,
        ControllerButton.DPadLeft, ControllerButton.DPadRight,
    ];

    /// <summary>Splits a set of flags into the individual buttons it contains.</summary>
    public static IEnumerable<ControllerButton> Split(ControllerButton buttons) =>
        All.Where(b => buttons.HasFlag(b));
}
