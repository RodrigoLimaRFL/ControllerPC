using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorNudgeTests
{
    private const int NudgeStep = 3;
    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton Right = ControllerButton.DPadRight;
    private const ControllerButton Up = ControllerButton.DPadUp;

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);
    private static readonly TimeSpan HeldLongEnoughToReachMinimumInterval = TimeSpan.FromSeconds(10);

    private readonly TranslatorHarness _harness = new();

    public DesktopInputTranslatorNudgeTests()
    {
        _harness.Profile.Tuning.NudgeStep = NudgeStep;
        _harness.Bind(Right, new NudgeAction(NudgeDirection.Right)).Bind(Up, new NudgeAction(NudgeDirection.Up));
    }

    [Theory]
    [InlineData(NudgeDirection.Up, 0, -NudgeStep)]
    [InlineData(NudgeDirection.Down, 0, NudgeStep)]
    [InlineData(NudgeDirection.Left, -NudgeStep, 0)]
    [InlineData(NudgeDirection.Right, NudgeStep, 0)]
    public void Update_NudgeBindingPressed_MovesOneStepInItsScreenDirection(NudgeDirection direction, int dx, int dy)
    {
        _harness.Bind(ControllerButton.A, new NudgeAction(direction)).Begin();

        _harness.Hold(ControllerButton.A, TimeSpan.Zero);

        Assert.Equal([(dx, dy)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_NudgeHeldShorterThanInitialDelay_DoesNotRepeat()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);

        _harness.Hold(Right, NudgeRepeatTimer.InitialRepeatDelay - OneTick);

        Assert.Equal(NudgeStep, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_NudgeHeldForInitialDelay_RepeatsOnce()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);

        _harness.Hold(Right, NudgeRepeatTimer.InitialRepeatDelay);

        Assert.Equal(2 * NudgeStep, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_NudgeHeldAfterFirstRepeat_RepeatsAfterFirstRepeatInterval()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(Right, NudgeRepeatTimer.InitialRepeatDelay);
        _harness.Hold(Right, NudgeRepeatTimer.FirstRepeatInterval - OneTick);
        Assert.Equal(2 * NudgeStep, _harness.Sink.TotalDx);

        _harness.Hold(Right, OneTick);

        Assert.Equal(3 * NudgeStep, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_NudgeHeld_RepeatIntervalShortens()
    {
        var secondInterval = TimeSpan.FromTicks(
            (long)(NudgeRepeatTimer.FirstRepeatInterval.Ticks * NudgeRepeatTimer.RepeatIntervalShrinkFactor));
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(Right, NudgeRepeatTimer.InitialRepeatDelay);
        _harness.Hold(Right, NudgeRepeatTimer.FirstRepeatInterval);
        _harness.Hold(Right, secondInterval - OneTick);
        Assert.Equal(3 * NudgeStep, _harness.Sink.TotalDx);

        _harness.Hold(Right, OneTick);

        Assert.Equal(4 * NudgeStep, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_NudgeHeldLong_RepeatsOncePerMinimumInterval()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(Right, HeldLongEnoughToReachMinimumInterval);
        _harness.Sink.CursorMoves.Clear();

        _harness.Hold(Right, NudgeRepeatTimer.MinimumRepeatInterval);
        _harness.Hold(Right, NudgeRepeatTimer.MinimumRepeatInterval);
        _harness.Hold(Right, NudgeRepeatTimer.MinimumRepeatInterval);

        Assert.Equal([(NudgeStep, 0), (NudgeStep, 0), (NudgeStep, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_LongUpdateWhileNudgeHeld_PerformsEveryRepeatThatFellDue()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);

        _harness.Hold(Right, NudgeRepeatTimer.InitialRepeatDelay + NudgeRepeatTimer.FirstRepeatInterval);

        Assert.Equal([(NudgeStep, 0), (2 * NudgeStep, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_NudgeReleased_StopsRepeating()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(None, TimeSpan.Zero);

        _harness.Hold(None, HeldLongEnoughToReachMinimumInterval);

        Assert.Equal(NudgeStep, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_NudgePressedAgain_StartsWithInitialDelayAgain()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(Right, HeldLongEnoughToReachMinimumInterval);
        _harness.Hold(None, TimeSpan.Zero);
        _harness.Sink.CursorMoves.Clear();

        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(Right, NudgeRepeatTimer.InitialRepeatDelay - OneTick);

        Assert.Equal([(NudgeStep, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_TwoNudgesHeld_EachRepeatsOnItsOwnSchedule()
    {
        var halfDelay = NudgeRepeatTimer.InitialRepeatDelay / 2;
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Hold(Right, halfDelay);
        _harness.Hold(Right | Up, TimeSpan.Zero);

        _harness.Hold(Right | Up, halfDelay);

        Assert.Equal(2 * NudgeStep, _harness.Sink.TotalDx);
        Assert.Equal(-NudgeStep, _harness.Sink.TotalDy);
    }

    [Fact]
    public void Update_NudgePressed_ElapsedTimeBeforePressDoesNotCountTowardRepeat()
    {
        _harness.Begin();

        _harness.Hold(Right, HeldLongEnoughToReachMinimumInterval);

        Assert.Equal(NudgeStep, _harness.Sink.TotalDx);
    }
}
