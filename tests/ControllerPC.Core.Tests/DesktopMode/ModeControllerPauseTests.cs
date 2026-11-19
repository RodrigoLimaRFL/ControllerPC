using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.DesktopMode.ModeControllerHarness;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class ModeControllerPauseTests
{
    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;

    private readonly ModeControllerHarness _harness = new();

    public ModeControllerPauseTests()
    {
        _harness.Main.Bindings[A] = new MouseButtonAction(MouseButton.Left);
        _harness.Main.Tuning.CursorSpeed = 1000;
        _harness.Main.Tuning.AccelerationEnabled = false;
        _harness.Start();
    }

    [Fact]
    public void Pause_SetsIsPausedAndRaisesStateChanged()
    {
        _harness.Controller.Pause();

        Assert.True(_harness.Controller.IsPaused);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Pause_InDesktopMode_ReleasesEverythingHeldAndStaysInDesktopMode()
    {
        EnterAndHoldA();

        _harness.Controller.Pause();

        Assert.Empty(_harness.Sink.MouseButtonsDown);
        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_WhilePausedInDesktopMode_TranslatesNothing()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Controller.Pause();

        _harness.Hold(0, A);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_WhilePausedInDesktopMode_DoesNotExit()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Controller.Pause();

        _harness.Hold(0, ExitChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_WhilePausedOutsideDesktopMode_DoesNotEnter()
    {
        _harness.Controller.Pause();

        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_WhilePausedOutsideDesktopMode_ReadsNoController()
    {
        _harness.Controller.Pause();
        var readsBefore = _harness.Source.ReadCount(0);

        _harness.Tick();

        Assert.Equal(readsBefore, _harness.Source.ReadCount(0));
    }

    [Fact]
    public void Tick_WhilePausedDrivingControllerDisconnects_LeavesDesktopMode()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Controller.Pause();
        _harness.Source.Disconnect(0);

        _harness.Tick();

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Resume_ClearsIsPausedAndRaisesStateChanged()
    {
        _harness.Controller.Pause();
        _harness.ClearRecordings();

        _harness.Controller.Resume(_harness.Now);

        Assert.False(_harness.Controller.IsPaused);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Resume_InDesktopMode_TranslatesAgain()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Controller.Pause();

        _harness.Controller.Resume(_harness.Now);
        _harness.Hold(0, A);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Resume_InDesktopModeWithButtonHeld_IgnoresItUntilReleased()
    {
        EnterAndHoldA();
        _harness.Controller.Pause();
        _harness.Sink.ButtonEvents.Clear();

        _harness.Controller.Resume(_harness.Now);
        _harness.Hold(0, A);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Resume_InDesktopModeWithExitChordHeld_DoesNotExitUntilReleased()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Controller.Pause();
        _harness.Hold(0, ExitChord);

        _harness.Controller.Resume(_harness.Now);
        _harness.Hold(0, ExitChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Resume_OutsideDesktopModeWithEntryChordHeld_DoesNotEnterUntilReleased()
    {
        _harness.Controller.Pause();
        _harness.Hold(0, EntryChord);

        _harness.Controller.Resume(_harness.Now);
        _harness.Hold(0, EntryChord);
        Assert.False(_harness.Controller.IsDesktopModeActive);

        _harness.Hold(0, None);
        _harness.Hold(0, EntryChord);
        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_FirstTickAfterResume_DoesNotCountThePausedTime()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Controller.Pause();
        _harness.TickFor(TimeSpan.FromSeconds(5));
        _harness.Source.Set(0, new ControllerState(None, new StickPosition(1, 0), StickPosition.Center));

        _harness.Controller.Resume(_harness.Now);
        _harness.Tick();

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Resume_WhenNotPaused_DoesNothing()
    {
        _harness.Controller.Resume(_harness.Now);

        Assert.Equal(0, _harness.StateChangedCount);
    }

    private void EnterAndHoldA()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Hold(0, A);
    }
}
