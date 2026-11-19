using System.Runtime.InteropServices;

namespace ControllerPC.Windows.XInput;

/// <summary><c>XINPUT_GAMEPAD</c> (12 bytes).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XInputGamepad
{
    /// <summary><c>wButtons</c>: bitmask of the digital buttons.</summary>
    public ushort Buttons;

    /// <summary><c>bLeftTrigger</c>: 0 (released) to 255 (fully pressed).</summary>
    public byte LeftTrigger;

    /// <summary><c>bRightTrigger</c>: 0 (released) to 255 (fully pressed).</summary>
    public byte RightTrigger;

    /// <summary><c>sThumbLX</c>: -32768 (left) to 32767 (right).</summary>
    public short LeftThumbX;

    /// <summary><c>sThumbLY</c>: -32768 (down) to 32767 (up).</summary>
    public short LeftThumbY;

    /// <summary><c>sThumbRX</c>: -32768 (left) to 32767 (right).</summary>
    public short RightThumbX;

    /// <summary><c>sThumbRY</c>: -32768 (down) to 32767 (up).</summary>
    public short RightThumbY;
}
