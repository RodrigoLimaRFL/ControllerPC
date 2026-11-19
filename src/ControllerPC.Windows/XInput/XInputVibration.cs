using System.Runtime.InteropServices;

namespace ControllerPC.Windows.XInput;

/// <summary><c>XINPUT_VIBRATION</c> (4 bytes). Motor speeds range from 0 (off) to 65535 (full).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XInputVibration
{
    /// <summary><c>wLeftMotorSpeed</c>: the low-frequency motor.</summary>
    public ushort LeftMotorSpeed;

    /// <summary><c>wRightMotorSpeed</c>: the high-frequency motor.</summary>
    public ushort RightMotorSpeed;
}
