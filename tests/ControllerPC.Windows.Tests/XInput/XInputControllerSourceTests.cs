using ControllerPC.Core.Controller;
using ControllerPC.Windows.XInput;

namespace ControllerPC.Windows.Tests.XInput;

public class XInputControllerSourceTests
{
    private const ushort GuideButtonBit = 0x0400;
    private const ushort ReservedButtonBit = 0x0800;

    [Fact]
    public void ToButtons_KeepsDigitalButtonBits()
    {
        ushort buttons = (ushort)(ControllerButton.A | ControllerButton.DPadLeft | ControllerButton.Back);

        ControllerButton result = XInputControllerSource.ToButtons(buttons, leftTrigger: 0, rightTrigger: 0);

        Assert.Equal(ControllerButton.A | ControllerButton.DPadLeft | ControllerButton.Back, result);
    }

    [Fact]
    public void ToButtons_DropsBitsWithoutAControllerButton()
    {
        ushort buttons = (ushort)((ushort)ControllerButton.Y | GuideButtonBit | ReservedButtonBit);

        ControllerButton result = XInputControllerSource.ToButtons(buttons, leftTrigger: 0, rightTrigger: 0);

        Assert.Equal(ControllerButton.Y, result);
    }

    [Fact]
    public void ToButtons_MapsEveryXInputBitToItsControllerButton()
    {
        ControllerButton result = XInputControllerSource.ToButtons(ushort.MaxValue, leftTrigger: 0, rightTrigger: 0);

        ControllerButton expected = ControllerButtons.All
            .Where(b => b is not ControllerButton.LeftTrigger and not ControllerButton.RightTrigger)
            .Aggregate(ControllerButton.None, (all, b) => all | b);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToButtons_AddsTriggersPressedPastThreshold()
    {
        ControllerButton result = XInputControllerSource.ToButtons(0, leftTrigger: 31, rightTrigger: 255);

        Assert.Equal(ControllerButton.LeftTrigger | ControllerButton.RightTrigger, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void IsTriggerPressed_IsFalseUpToThreshold(byte trigger)
    {
        Assert.False(XInputControllerSource.IsTriggerPressed(trigger));
    }

    [Fact]
    public void IsTriggerPressed_IsTrueAboveThreshold()
    {
        Assert.True(XInputControllerSource.IsTriggerPressed(31));
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(short.MaxValue, 1.0)]
    [InlineData(-short.MaxValue, -1.0)]
    [InlineData(short.MinValue, -1.0)]
    public void NormalizeAxis_MapsRawRangeOntoUnitRange(short raw, double expected)
    {
        Assert.Equal(expected, XInputControllerSource.NormalizeAxis(raw));
    }

    [Fact]
    public void NormalizeAxis_ScalesBothDirectionsEqually()
    {
        Assert.Equal(-XInputControllerSource.NormalizeAxis(16384), XInputControllerSource.NormalizeAxis(-16384));
    }

    [Fact]
    public void ToControllerState_KeepsYUpPositiveAndAssignsEachStick()
    {
        var gamepad = new XInputGamepad
        {
            LeftThumbX = short.MaxValue,
            LeftThumbY = 0,
            RightThumbX = 0,
            RightThumbY = short.MaxValue,
        };

        ControllerState state = XInputControllerSource.ToControllerState(gamepad);

        Assert.Equal(new StickPosition(1, 0), state.LeftStick);
        Assert.Equal(new StickPosition(0, 1), state.RightStick);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Read_RejectsSlotOutsideXInputRange(int slot)
    {
        var source = new XInputControllerSource();

        Assert.Throws<ArgumentOutOfRangeException>(() => source.Read(slot));
    }
}
