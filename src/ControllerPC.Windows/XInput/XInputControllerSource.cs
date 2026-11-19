using System.Diagnostics;
using ControllerPC.Core.Controller;

namespace ControllerPC.Windows.XInput;

/// <summary>
/// Reads Xbox-style controllers through XInput (xinput1_4.dll). Masks <c>wButtons</c> to the known
/// <see cref="ControllerButton"/> flags, maps LT/RT to buttons past <c>XINPUT_GAMEPAD_TRIGGER_THRESHOLD</c>,
/// and normalizes sticks to [-1, 1].
/// </summary>
public sealed class XInputControllerSource : IControllerSource
{
    /// <summary>
    /// The <c>wButtons</c> bits that have a <see cref="ControllerButton"/> counterpart. Other bits (0x0400 is the
    /// Guide button on some drivers, 0x0800 is reserved) are dropped so Core never sees unknown flags.
    /// </summary>
    private const ControllerButton DigitalButtons =
        ControllerButton.DPadUp | ControllerButton.DPadDown | ControllerButton.DPadLeft | ControllerButton.DPadRight
        | ControllerButton.Start | ControllerButton.Back
        | ControllerButton.LeftThumb | ControllerButton.RightThumb
        | ControllerButton.LeftShoulder | ControllerButton.RightShoulder
        | ControllerButton.A | ControllerButton.B | ControllerButton.X | ControllerButton.Y;

    public int SlotCount => XInputNative.MaxControllerCount;

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is not in [0, <see cref="SlotCount"/>).</exception>
    public ControllerState? Read(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, SlotCount);

        uint result = XInputNative.XInputGetState((uint)slot, out XInputState state);
        switch (result)
        {
            case XInputNative.ErrorSuccess:
                return ToControllerState(state.Gamepad);
            case XInputNative.ErrorDeviceNotConnected:
                return null;
            default:
                Trace.TraceWarning($"XInputGetState({slot}) failed with error {result}.");
                return null;
        }
    }

    internal static ControllerState ToControllerState(in XInputGamepad gamepad) =>
        new(
            ToButtons(gamepad.Buttons, gamepad.LeftTrigger, gamepad.RightTrigger),
            new StickPosition(NormalizeAxis(gamepad.LeftThumbX), NormalizeAxis(gamepad.LeftThumbY)),
            new StickPosition(NormalizeAxis(gamepad.RightThumbX), NormalizeAxis(gamepad.RightThumbY)));

    internal static ControllerButton ToButtons(ushort buttons, byte leftTrigger, byte rightTrigger)
    {
        ControllerButton result = (ControllerButton)buttons & DigitalButtons;
        if (IsTriggerPressed(leftTrigger))
        {
            result |= ControllerButton.LeftTrigger;
        }

        if (IsTriggerPressed(rightTrigger))
        {
            result |= ControllerButton.RightTrigger;
        }

        return result;
    }

    internal static bool IsTriggerPressed(byte trigger) => trigger > XInputNative.TriggerThreshold;

    /// <summary>
    /// Maps a raw thumbstick axis to [-1, 1]. The raw range is asymmetric (-32768..32767); -32768 is clamped to
    /// -32767 so both directions scale by the same factor and full deflection is exactly ±1. XInput already
    /// reports Y as up-positive, which matches <see cref="StickPosition"/>.
    /// </summary>
    internal static double NormalizeAxis(short raw) =>
        Math.Max(raw, (short)-short.MaxValue) / (double)short.MaxValue;
}
