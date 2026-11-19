using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorStickTests
{
    private static readonly TimeSpan HalfSecond = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

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
        _harness.Sticks(new StickPosition(0.15, 0.1), Center, OneSecond);

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LeftStickFullRight_MovesRightAtCursorSpeed()
    {
        _harness.Sticks(FullRight, Center, HalfSecond);

        Assert.Equal([(500, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LeftStickUp_MovesCursorUpOnScreen()
    {
        _harness.Sticks(FullUp, Center, HalfSecond);

        Assert.Equal([(0, -500)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LeftStickBeyondUnitCircle_MovesNoFasterThanFullDeflection()
    {
        _harness.Sticks(new StickPosition(1, 1), Center, OneSecond);

        var distance = Math.Sqrt((_harness.Sink.TotalDx * _harness.Sink.TotalDx) + (_harness.Sink.TotalDy * _harness.Sink.TotalDy));
        Assert.InRange(distance, TranslatorHarness.CursorSpeed - 1, TranslatorHarness.CursorSpeed);
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

        _harness.Sticks(new StickPosition(0.75, 0), Center, OneSecond);

        Assert.Equal(500, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_AccelerationEnabled_SpeedIsSquaredRescaledDeflection()
    {
        _harness.Profile.Tuning.Deadzone = 0.5;
        _harness.Profile.Tuning.AccelerationEnabled = true;

        _harness.Sticks(new StickPosition(0.75, 0), Center, OneSecond);

        Assert.Equal(250, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_AccelerationEnabled_FullDeflectionStillMovesAtCursorSpeed()
    {
        _harness.Profile.Tuning.AccelerationEnabled = true;

        _harness.Sticks(FullRight, Center, HalfSecond);

        Assert.Equal(500, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_ScrollRoleRightStickUp_ScrollsUpAtScrollSpeed()
    {
        _harness.Sticks(Center, FullUp, HalfSecond);

        Assert.Equal([(600, 0)], _harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ScrollRoleRightStickRight_ScrollsRight()
    {
        _harness.Sticks(Center, FullRight, HalfSecond);

        Assert.Equal([(0, 600)], _harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_ScrollRole_RightStickDoesNotMoveCursor()
    {
        _harness.Sticks(Center, FullRight, HalfSecond);

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_ScrollRoleRightStickInsideDeadzone_DoesNotScroll()
    {
        _harness.Sticks(Center, new StickPosition(0, 0.15), OneSecond);

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

        _harness.Sticks(Center, new StickPosition(0, 0.75), OneSecond);

        Assert.Equal(600, _harness.Sink.TotalVerticalScroll);
    }

    [Fact]
    public void Update_CursorRole_RightStickMovesCursor()
    {
        _harness.Profile.RightStickRole = RightStickRole.Cursor;

        _harness.Sticks(Center, FullUp, HalfSecond);

        Assert.Equal([(0, -500)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_CursorRole_RightStickDoesNotScroll()
    {
        _harness.Profile.RightStickRole = RightStickRole.Cursor;

        _harness.Sticks(Center, FullUp, HalfSecond);

        Assert.Empty(_harness.Sink.Scrolls);
    }

    [Fact]
    public void Update_CursorRole_AddsBothSticks()
    {
        _harness.Profile.RightStickRole = RightStickRole.Cursor;

        _harness.Sticks(FullRight, FullRight, HalfSecond);

        Assert.Equal([(1000, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_NoneRole_RightStickDoesNothing()
    {
        _harness.Profile.RightStickRole = RightStickRole.None;

        _harness.Sticks(Center, FullUp, HalfSecond);

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

        _harness.Sticks(FullRight, Center, HalfSecond);

        Assert.Equal([(500, 0)], _harness.Sink.CursorMoves);
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
