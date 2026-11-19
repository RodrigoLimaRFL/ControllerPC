using ControllerPC.Core.Controller;

namespace ControllerPC.Windows.XInput;

/// <summary>
/// Reads Xbox-style controllers through XInput (xinput1_4.dll). Masks <c>wButtons</c> to the known
/// <see cref="ControllerButton"/> flags, maps LT/RT to buttons past <c>XINPUT_GAMEPAD_TRIGGER_THRESHOLD</c>,
/// and normalizes sticks to [-1, 1].
/// </summary>
public sealed class XInputControllerSource : IControllerSource
{
    private const int XUserMaxCount = 4;

    public int SlotCount => XUserMaxCount;

    public ControllerState? Read(int slot) => throw new NotImplementedException();
}
