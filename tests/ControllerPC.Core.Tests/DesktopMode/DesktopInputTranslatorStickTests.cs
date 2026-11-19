using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorStickTests
{
    // The longest update the translator integrates over: 100 px at CursorSpeed 1000 px/s, a wheel delta of 120
    // (one notch) at ScrollSpeed 10 notches/s.
    private static readonly TimeSpan LongestUpdate = DesktopInputTranslator.MaxUpdateElapsed;

    // At CursorSpeed 1000 px/s this is 0.4 px per update; at ScrollSpeed 10 notches/s it is a wheel delta of 0.48.
    private static readonly TimeSpan SubUnitStep = TimeSpan.FromTicks(4_000);

    private static readonly StickPosition FullRight = new(1, 0);
    private static readonly StickPosition FullUp = new(0, 1);
    private static readonly StickPosition Center = StickPosition.Center;

    private readonly TranslatorHarness _harness = new();

    public DesktopInputTranslatorStickTests()
    {
        _harness.Begin();
    }

    [Fact]
    public void Update_LeftStickInsideDeadzone_DoesNotMoveCursor()
    {
        _harness.Sticks(new StickPosition(0.15, 0.1), Center, LongestUpdate);

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LeftStickFullRight_MovesRightAtCursorSpeed()
    {
        _harness.Sticks(FullRight, Center, LongestUpdate);

        Assert.Equal([(100, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LeftStickUp_MovesCursorUpOnScreen()
    {
        _harness.Sticks(FullUp, Center, LongestUpdate);

        Assert.Equal([(0, -100)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LeftStickBeyondUnitCircle_MovesNoFasterThanFullDeflection()
    {
        const double FullDeflectionDistance = 100;
        const double TruncationTolerance = 2;

        _harness.Sticks(new StickPosition(1, 1), Center, LongestUpdate);

        var distance = Math.Sqrt((_harness.Sink.TotalDx * _harness.Sink.TotalDx) + (_harness.Sink.TotalDy * _harness.Sink.TotalDy));
        Assert.InRange(distance, FullDeflectionDistance - TruncationTolerance, FullDeflectionDistance);
    }

    [Fact]
    public void Update_SlowLeftStick_CarriesSubPixelRemainderAcrossUpdates()
    {
        _harness.Sticks(FullRight, Center, SubUnitStep);
        _harness.Sticks(FullRight, Center, SubUnitStep);
        Assert.Empty(_harness.Sink.CursorMoves);

        _harness.Sticks(FullRight, Center, SubUnitStep);

        Assert.Equal([(1, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_AccelerationDisabled_SpeedIsLinearInRescaledDeflection()
    {
        _harness.Profile.Tuning.Deadzone = 0.5;
        _harness.Profile.Tuning.AccelerationEnabled = false;

        _harness.Sticks(new StickPosition(0.75, 0), Center, LongestUpdate);

        Assert.Equal(50, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_AccelerationEnabled_SpeedIsSquaredRescaledDeflection()
    {
        _harness.Profile.Tuning.Deadzone = 0.5;
        _harness.Profile.Tuning.AccelerationEnabled = true;

        _harness.Sticks(new StickPosition(0.75, 0), Center, LongestUpdate);

        Assert.Equal(25, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_AccelerationEnabled_FullDeflectionStillMovesAtCursorSpeed()
    {
        _harness.Profile.Tuning.AccelerationEnabled = true;

        _harness.Sticks(FullRight, Center, LongestUpdate);

        Assert.Equal(100, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_NegativeElapsed_DoesNotMoveCursor()
    {
        _harness.Sticks(FullRight, Center, -LongestUpdate);

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_ElapsedLongerThanMaxUpdateElapsed_MovesAsFarAsMaxUpdateElapsed()
    {
        _harness.Sticks(FullRight, Center, TimeSpan.FromHours(1));

        Assert.Equal([(100, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_NegativeElapsed_DoesNotScroll()
    {
        _harness.Sticks(Center, FullUp, -LongestUpdate);

        Assert.Empty(_harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ElapsedLongerThanMaxUpdateElapsed_ScrollsAsFarAsMaxUpdateElapsed()
    {
        _harness.Sticks(Center, FullUp, TimeSpan.FromHours(1));

        Assert.Equal([(120, 0)], _harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ScrollRoleRightStickUp_ScrollsUpAtScrollSpeed()
    {
        _harness.Sticks(Center, FullUp, LongestUpdate);

        Assert.Equal([(120, 0)], _harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ScrollRoleRightStickRight_ScrollsRight()
    {
        _harness.Sticks(Center, FullRight, LongestUpdate);

        Assert.Equal([(0, 120)], _harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ScrollRole_RightStickDoesNotMoveCursor()
    {
        _harness.Sticks(Center, FullRight, LongestUpdate);

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_ScrollRoleRightStickInsideDeadzone_DoesNotScroll()
    {
        _harness.Sticks(Center, new StickPosition(0, 0.15), LongestUpdate);

        Assert.Empty(_harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_SlowScroll_CarriesSubNotchRemainderAcrossUpdates()
    {
        _harness.Sticks(Center, FullUp, SubUnitStep);
        _harness.Sticks(Center, FullUp, SubUnitStep);
        Assert.Empty(_harness.Sink.Scrolls);

        _harness.Sticks(Center, FullUp, SubUnitStep);

        Assert.Equal([(1, 0)], _harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ScrollRoleWithAccelerationEnabled_ScrollsLinearly()
    {
        _harness.Profile.Tuning.Deadzone = 0.5;
        _harness.Profile.Tuning.AccelerationEnabled = true;

        _harness.Sticks(Center, new StickPosition(0, 0.75), LongestUpdate);

        Assert.Equal(60, _harness.Sink.TotalVerticalScroll);
    }

    [Fact]
    public void Update_CursorRole_RightStickMovesCursor()
    {
        _harness.Profile.RightStickRole = RightStickRole.Cursor;

        _harness.Sticks(Center, FullUp, LongestUpdate);

        Assert.Equal([(0, -100)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_CursorRole_RightStickDoesNotScroll()
    {
        _harness.Profile.RightStickRole = RightStickRole.Cursor;

        _harness.Sticks(Center, FullUp, LongestUpdate);

        Assert.Empty(_harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_CursorRole_AddsBothSticks()
    {
        _harness.Profile.RightStickRole = RightStickRole.Cursor;

        _harness.Sticks(FullRight, FullRight, LongestUpdate);

        Assert.Equal([(200, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_NoneRole_RightStickDoesNothing()
    {
        _harness.Profile.RightStickRole = RightStickRole.None;

        _harness.Sticks(Center, FullUp, LongestUpdate);

        Assert.Empty(_harness.Sink.CursorMoves);
        Assert.Empty(_harness.Sink.Scrolls);
    }

    [Theory]
    [InlineData(RightStickRole.Scroll)]
    [InlineData(RightStickRole.Cursor)]
    [InlineData(RightStickRole.None)]
    public void Update_AnyRole_LeftStickMovesCursor(RightStickRole role)
    {
        _harness.Profile.RightStickRole = role;

        _harness.Sticks(FullRight, Center, LongestUpdate);

        Assert.Equal([(100, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Begin_DiscardsSubPixelRemainder()
    {
        _harness.Sticks(FullRight, Center, SubUnitStep);
        _harness.Sticks(FullRight, Center, SubUnitStep);

        _harness.Begin();
        _harness.Sticks(FullRight, Center, SubUnitStep);

        Assert.Empty(_harness.Sink.CursorMoves);
    }
}
