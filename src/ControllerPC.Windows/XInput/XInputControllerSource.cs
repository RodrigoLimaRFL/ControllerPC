using ControllerPC.Core.Controller;

namespace ControllerPC.Windows.XInput;

/// <summary>Reads Xbox-style controllers through XInput (xinput1_4.dll).</summary>
public sealed class XInputControllerSource : IControllerSource
{
    public int SlotCount => 4;

    public ControllerState? Read(int slot) => throw new NotImplementedException();
}
