using System.Runtime.InteropServices;
using ControllerPC.Windows.XInput;

namespace ControllerPC.Windows.Tests.XInput;

public class XInputNativeLayoutTests
{
    [Fact]
    public void XInputGamepad_Is12Bytes()
    {
        Assert.Equal(12, Marshal.SizeOf<XInputGamepad>());
    }

    [Fact]
    public void XInputGamepad_HasWin32FieldOffsets()
    {
        Assert.Equal(2, (int)Marshal.OffsetOf<XInputGamepad>(nameof(XInputGamepad.LeftTrigger)));
        Assert.Equal(4, (int)Marshal.OffsetOf<XInputGamepad>(nameof(XInputGamepad.LeftThumbX)));
        Assert.Equal(10, (int)Marshal.OffsetOf<XInputGamepad>(nameof(XInputGamepad.RightThumbY)));
    }

    [Fact]
    public void XInputState_Is16Bytes()
    {
        Assert.Equal(16, Marshal.SizeOf<XInputState>());
    }

    [Fact]
    public void XInputVibration_Is4Bytes()
    {
        Assert.Equal(4, Marshal.SizeOf<XInputVibration>());
    }
}
