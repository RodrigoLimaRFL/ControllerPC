using ControllerPC.Core.Controller;
using ControllerPC.Windows.XInput;

namespace ControllerPC.Windows.Tests.XInput;

public class XInputRumbleTests
{
    [Fact]
    public void DescribePulses_EnteredDesktopModeIsOneShortPulse()
    {
        (int count, TimeSpan duration) = XInputRumble.DescribePulses(RumblePattern.EnteredDesktopMode);
        (_, TimeSpan longDuration) = XInputRumble.DescribePulses(RumblePattern.ProfileSwitched);

        Assert.Equal(1, count);
        Assert.True(duration < longDuration);
    }

    [Fact]
    public void DescribePulses_ExitedDesktopModeIsTwoShortPulses()
    {
        (int count, TimeSpan duration) = XInputRumble.DescribePulses(RumblePattern.ExitedDesktopMode);
        (_, TimeSpan shortDuration) = XInputRumble.DescribePulses(RumblePattern.EnteredDesktopMode);

        Assert.Equal(2, count);
        Assert.Equal(shortDuration, duration);
    }

    [Fact]
    public void DescribePulses_ProfileSwitchedIsOnePulse()
    {
        (int count, _) = XInputRumble.DescribePulses(RumblePattern.ProfileSwitched);

        Assert.Equal(1, count);
    }

    [Fact]
    public void DescribePulses_RejectsUnknownPattern()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => XInputRumble.DescribePulses((RumblePattern)99));
    }
}
