using ControllerPC.Core.Output;
using ControllerPC.Windows.Output;

namespace ControllerPC.Windows.Tests.Output;

public class SendInputSinkTests
{
    private const ushort LeftArrow = 0x25;
    private const ushort LetterA = 0x41;

    [Fact]
    public void KeyDownFlags_SetsExtendedKeyForExtendedKeys()
    {
        Assert.Equal(User32Native.KeyEventExtendedKey, SendInputSink.KeyDownFlags(LeftArrow));
    }

    [Fact]
    public void KeyDownFlags_IsZeroForOtherKeys()
    {
        Assert.Equal(0u, SendInputSink.KeyDownFlags(LetterA));
    }

    [Theory]
    [InlineData(MouseButton.Left, User32Native.MouseEventLeftDown, User32Native.MouseEventLeftUp)]
    [InlineData(MouseButton.Right, User32Native.MouseEventRightDown, User32Native.MouseEventRightUp)]
    [InlineData(MouseButton.Middle, User32Native.MouseEventMiddleDown, User32Native.MouseEventMiddleUp)]
    public void MouseButtonFlags_UsesDedicatedFlagsWithoutMouseData(MouseButton button, uint down, uint up)
    {
        Assert.Equal((down, up, 0u), SendInputSink.MouseButtonFlags(button));
    }

    [Theory]
    [InlineData(MouseButton.Back, User32Native.XButton1)]
    [InlineData(MouseButton.Forward, User32Native.XButton2)]
    public void MouseButtonFlags_UsesXButtonFlagsWithButtonInMouseData(MouseButton button, uint mouseData)
    {
        Assert.Equal(
            (User32Native.MouseEventXDown, User32Native.MouseEventXUp, mouseData),
            SendInputSink.MouseButtonFlags(button));
    }

    [Fact]
    public void MouseButtonFlags_RejectsUnknownButton()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SendInputSink.MouseButtonFlags((MouseButton)99));
    }
}
