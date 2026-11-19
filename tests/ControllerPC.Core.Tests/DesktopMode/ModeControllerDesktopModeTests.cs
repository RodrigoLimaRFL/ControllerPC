using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.DesktopMode.ModeControllerHarness;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class ModeControllerDesktopModeTests
{
    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton Y = ControllerButton.Y;
    private const ushort KeyF = 0x46;

    private readonly ModeControllerHarness _harness = new();

    public ModeControllerDesktopModeTests()
    {
        _harness.Main.Bindings[A] = new MouseButtonAction(MouseButton.Left);
        _harness.Main.Bindings[Y] = new NextProfileAction();
        _harness.Second.Bindings[A] = new MouseButtonAction(MouseButton.Right);
        _harness.Source.Connect(1);
        _harness.Start();
        _harness.EnterWithBaseTrigger();
        _harness.ClearRecordings();
    }

    [Fact]
    public void Tick_InDesktopMode_TranslatesTheDrivingController()
    {
        _harness.Hold(0, A);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_InDesktopMode_IgnoresOtherControllers()
    {
        _harness.Hold(1, A);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_InDesktopMode_DoesNotReadOtherControllers()
    {
        var readsBefore = _harness.Source.ReadCount(1);

        _harness.TickFor(TimeSpan.FromSeconds(3));

        Assert.Equal(readsBefore, _harness.Source.ReadCount(1));
    }

    [Fact]
    public void Tick_OtherControllerCompletesExitTrigger_StaysInDesktopMode()
    {
        _harness.Hold(1, ExitChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_OtherControllerCompletesEntryTrigger_KeepsDrivingController()
    {
        _harness.Hold(1, None);
        _harness.Hold(1, EntryChord);

        Assert.Equal(0, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_ExitTriggerCompleted_LeavesDesktopMode()
    {
        _harness.Hold(0, ExitChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
        Assert.Null(_harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_ExitTriggerCompleted_RumblesExitedDesktopModeOnTheDrivingController()
    {
        _harness.Hold(0, ExitChord);

        Assert.Equal([(0, RumblePattern.ExitedDesktopMode)], _harness.Rumble.Played);
    }

    [Fact]
    public void Tick_ExitTriggerCompleted_RaisesStateChanged()
    {
        _harness.Hold(0, ExitChord);

        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Tick_ExitTriggerCompleted_ReleasesEverythingHeld()
    {
        _harness.Main.Bindings[ControllerButton.Back] = new MouseButtonAction(MouseButton.Middle);
        _harness.Hold(0, ControllerButton.Back);

        _harness.Hold(0, ExitChord);

        Assert.Empty(_harness.Sink.MouseButtonsDown);
    }

    [Fact]
    public void Tick_ExitTriggerCompleted_DoesNotTranslateThatState()
    {
        _harness.Main.Bindings[ControllerButton.Start] = new KeyCombinationAction(new KeyCombination([KeyF]));
        _harness.Hold(0, ControllerButton.Back);

        _harness.Hold(0, ExitChord);

        Assert.Empty(_harness.Sink.KeyCalls);
    }

    [Fact]
    public void Tick_AfterExiting_TranslatesNothing()
    {
        _harness.Hold(0, ExitChord);
        _harness.Hold(0, None);

        _harness.Hold(0, A);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_AfterExiting_EntryTriggerWorksOnAnyController()
    {
        _harness.Hold(0, ExitChord);
        _harness.Hold(0, None);

        _harness.Hold(1, EntryChord);

        Assert.Equal(1, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_ActiveProfileHasExitOverride_UsesIt()
    {
        _harness.Main.ExitTriggerOverride = Trigger(ControllerButton.B);
        _harness.Controller.ApplySettings(_harness.Settings, _harness.Now);

        _harness.Hold(0, ControllerButton.B);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_DrivingControllerDisconnects_LeavesDesktopMode()
    {
        _harness.Source.Disconnect(0);

        _harness.Tick();

        Assert.False(_harness.Controller.IsDesktopModeActive);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Tick_DrivingControllerDisconnects_ReleasesEverythingHeld()
    {
        _harness.Hold(0, A);
        _harness.Source.Disconnect(0);

        _harness.Tick();

        Assert.Empty(_harness.Sink.MouseButtonsDown);
    }

    [Fact]
    public void Tick_DrivingControllerDisconnects_DoesNotRumble()
    {
        _harness.Source.Disconnect(0);

        _harness.Tick();

        Assert.Empty(_harness.Rumble.Played);
    }

    [Fact]
    public void Tick_NextProfilePressed_MakesTheNextProfileActive()
    {
        _harness.Hold(0, Y);

        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
        Assert.Equal(_harness.Second.Id, _harness.Settings.ActiveProfileId);
    }

    [Fact]
    public void Tick_NextProfilePressedOnLastProfile_WrapsToTheFirst()
    {
        _harness.Second.Bindings[Y] = new NextProfileAction();
        _harness.Tap(0, Y);

        _harness.Hold(0, Y);

        Assert.Same(_harness.Main, _harness.Controller.ActiveProfile);
    }

    [Fact]
    public void Tick_ProfileSwitched_RumblesProfileSwitchedAndRaisesStateChanged()
    {
        _harness.Hold(0, Y);

        Assert.Equal([(0, RumblePattern.ProfileSwitched)], _harness.Rumble.Played);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Tick_ProfileSwitched_StaysInDesktopMode()
    {
        _harness.Hold(0, Y);

        Assert.True(_harness.Controller.IsDesktopModeActive);
        Assert.Equal(0, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_ProfileSwitched_TranslatesWithTheNewProfile()
    {
        _harness.Tap(0, Y);

        _harness.Hold(0, A);

        Assert.Equal([MouseEvent("down", MouseButton.Right)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_ProfileSwitched_ButtonsHeldAtTheSwitchAreIgnoredUntilReleased()
    {
        _harness.Hold(0, A);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(0, A | Y);
        _harness.Hold(0, A);

        Assert.Equal([MouseEvent("up", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_ProfileSwitched_UsesTheNewProfilesExitTrigger()
    {
        _harness.Second.ExitTriggerOverride = Trigger(ControllerButton.B);
        _harness.Tap(0, Y);

        _harness.Hold(0, ControllerButton.B);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_SwitchToProfilePressed_MakesThatProfileActive()
    {
        _harness.Main.Bindings[ControllerButton.X] = new SwitchToProfileAction(_harness.Second.Id);

        _harness.Hold(0, ControllerButton.X);

        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
    }

    [Fact]
    public void Tick_SwitchToUnknownProfilePressed_DoesNothing()
    {
        _harness.Main.Bindings[ControllerButton.X] = new SwitchToProfileAction(Guid.NewGuid());

        _harness.Hold(0, ControllerButton.X);

        Assert.Same(_harness.Main, _harness.Controller.ActiveProfile);
        Assert.Empty(_harness.Rumble.Played);
        Assert.Equal(0, _harness.StateChangedCount);
    }

    [Fact]
    public void Tick_SwitchToActiveProfilePressed_DoesNothing()
    {
        _harness.Main.Bindings[ControllerButton.X] = new SwitchToProfileAction(_harness.Main.Id);
        _harness.Hold(0, A);

        _harness.Hold(0, A | ControllerButton.X);

        Assert.Empty(_harness.Rumble.Played);
        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_NextProfileWithSingleProfile_DoesNothing()
    {
        _harness.Settings.Profiles.Remove(_harness.Second);

        _harness.Hold(0, Y);

        Assert.Same(_harness.Main, _harness.Controller.ActiveProfile);
        Assert.Empty(_harness.Rumble.Played);
    }

    [Fact]
    public void Tick_FirstTickAfterEntering_HasNoElapsedTime()
    {
        _harness.Main.Tuning.CursorSpeed = 1000;
        _harness.Main.Tuning.AccelerationEnabled = false;
        _harness.Hold(0, ExitChord);
        _harness.Hold(0, None);
        _harness.Hold(0, EntryChord);
        _harness.Sink.CursorMoves.Clear();
        _harness.Source.Set(0, new ControllerState(EntryChord, new StickPosition(1, 0), StickPosition.Center));

        _harness.Tick();

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Tick_LaterTicks_ElapsedIsTimeSincePreviousTick()
    {
        _harness.Main.Tuning.CursorSpeed = 1000;
        _harness.Main.Tuning.AccelerationEnabled = false;
        _harness.Source.Set(0, new ControllerState(None, new StickPosition(1, 0), StickPosition.Center));

        _harness.Tick();

        // 10 ms (one Poll) at 1000 px/s.
        Assert.Equal(10, _harness.Sink.TotalDx);
    }
}
