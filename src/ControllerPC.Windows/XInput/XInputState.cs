using System.Runtime.InteropServices;

namespace ControllerPC.Windows.XInput;

/// <summary><c>XINPUT_STATE</c> (16 bytes).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XInputState
{
    /// <summary><c>dwPacketNumber</c>: changes whenever the controller state changes.</summary>
    public uint PacketNumber;

    public XInputGamepad Gamepad;
}
