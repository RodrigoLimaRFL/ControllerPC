using System.Runtime.InteropServices;
using ControllerPC.Windows.Output;

namespace ControllerPC.Windows.Tests.Output;

public class NativeInputLayoutTests
{
    [Fact]
    public void NativeInput_MatchesWin32SizeForProcessArchitecture()
    {
        int expected = Environment.Is64BitProcess ? 40 : 28;

        Assert.Equal(expected, Marshal.SizeOf<NativeInput>());
    }

    [Fact]
    public void NativeInput_UnionStartsAtPointerAlignment()
    {
        Assert.Equal(IntPtr.Size, (int)Marshal.OffsetOf<NativeInput>(nameof(NativeInput.Data)));
    }

    [Fact]
    public void NativeInputUnion_IsSizeOfMouseInput()
    {
        Assert.Equal(Marshal.SizeOf<NativeMouseInput>(), Marshal.SizeOf<NativeInputUnion>());
    }

    [Fact]
    public void NativeKeyboardInput_MatchesWin32SizeForProcessArchitecture()
    {
        int expected = Environment.Is64BitProcess ? 24 : 16;

        Assert.Equal(expected, Marshal.SizeOf<NativeKeyboardInput>());
    }

    [Fact]
    public void NativeHardwareInput_Is8Bytes()
    {
        Assert.Equal(8, Marshal.SizeOf<NativeHardwareInput>());
    }
}
