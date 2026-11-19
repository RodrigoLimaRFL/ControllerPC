using ControllerPC.Windows.Output;

namespace ControllerPC.Windows.Tests.Output;

public class ExtendedKeysTests
{
    [Theory]
    [InlineData(0x21)] // Page Up
    [InlineData(0x22)] // Page Down
    [InlineData(0x23)] // End
    [InlineData(0x24)] // Home
    [InlineData(0x25)] // Left Arrow
    [InlineData(0x26)] // Up Arrow
    [InlineData(0x27)] // Right Arrow
    [InlineData(0x28)] // Down Arrow
    [InlineData(0x2D)] // Insert
    [InlineData(0x2E)] // Delete
    [InlineData(0x5B)] // Left Windows
    [InlineData(0x5C)] // Right Windows
    [InlineData(0x5D)] // Applications
    [InlineData(0x6F)] // Numpad Divide
    [InlineData(0x90)] // Num Lock
    [InlineData(0xA3)] // Right Ctrl
    [InlineData(0xA5)] // Right Alt
    [InlineData(0xAD)] // Volume Mute
    [InlineData(0xB3)] // Media Play/Pause
    public void Contains_IsTrueForKeysWithE0Prefix(ushort virtualKey)
    {
        Assert.True(ExtendedKeys.Contains(virtualKey));
    }

    [Theory]
    [InlineData(0x08)] // Backspace
    [InlineData(0x09)] // Tab
    [InlineData(0x0D)] // Enter
    [InlineData(0x10)] // Shift
    [InlineData(0x11)] // Ctrl
    [InlineData(0x12)] // Alt
    [InlineData(0x1B)] // Escape
    [InlineData(0x20)] // Space
    [InlineData(0x41)] // A
    [InlineData(0x60)] // Numpad 0
    [InlineData(0x70)] // F1
    [InlineData(0xA0)] // Left Shift
    [InlineData(0xA1)] // Right Shift
    [InlineData(0xA2)] // Left Ctrl
    [InlineData(0xA4)] // Left Alt
    public void Contains_IsFalseForOtherKeys(ushort virtualKey)
    {
        Assert.False(ExtendedKeys.Contains(virtualKey));
    }
}
