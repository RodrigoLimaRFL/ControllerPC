using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.DesktopMode.ModeControllerHarness;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class ModeControllerEntryTests
{
    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton OverrideEntryChord = ControllerButton.X | ControllerButton.Y;

    private readonly ModeControllerHarness _harness = new();

    [Fact]
    public void Constructor_StartsOutsideDesktopMode()
    {
        Assert.False(_harness.Controller.IsDesktopModeActive);
        Assert.Null(_harness.Controller.DrivingSlot);
        Assert.False(_harness.Controller.IsPaused);
    }

    [Fact]
    public void Tick_BaseEntryTriggerCompleted_EntersDesktopMode()
    {
        _harness.Start();

        _harness.Hold(0, EntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_EntryTriggerCompletedOnSlot_MakesThatSlotTheDrivingController()
    {
        _harness.Source.Connect(2);
        _harness.Start();

        _harness.Hold(2, EntryChord);

        Assert.Equal(2, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_Entering_RumblesEnteredDesktopModeOnTheDrivingController()
    {
        _harness.Source.Connect(2);
        _harness.Start();

        _harness.Hold(2, EntryChord);

        Assert.Equal([(2, RumblePattern.EnteredDesktopMode)], _harness.Rumble.Played);
    }

    [Fact]
    public void Tick_Entering_RaisesStateChangedOnce()
    {
        _harness.Start();

        _harness.Hold(0, EntryChord);

        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Tick_BaseEntryTriggerCompleted_KeepsTheActiveProfile()
    {
        _harness.Settings.ActiveProfileId = _harness.Second.Id;
        _harness.Start();

        _harness.Hold(0, EntryChord);

        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
    }

    [Fact]
    public void Tick_EntryChordHeldWhenSlotIsFirstSeen_DoesNotEnterUntilReleased()
    {
        _harness.Source.Set(0, EntryChord);
        _harness.Start();
        _harness.Tick();
        Assert.False(_harness.Controller.IsDesktopModeActive);

        _harness.Hold(0, None);
        _harness.Hold(0, EntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_EntryOverrideCompleted_MakesItsProfileActive()
    {
        _harness.Second.EntryTriggerOverride = Trigger(OverrideEntryChord);
        _harness.Start();

        _harness.Hold(0, OverrideEntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
        Assert.Equal(_harness.Second.Id, _harness.Settings.ActiveProfileId);
    }

    [Fact]
    public void Tick_EntryOverrideOfInactiveProfile_DoesNotStopBaseEntryTrigger()
    {
        _harness.Second.EntryTriggerOverride = Trigger(OverrideEntryChord);
        _harness.Start();

        _harness.Hold(0, EntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
        Assert.Same(_harness.Main, _harness.Controller.ActiveProfile);
    }

    [Fact]
    public void Tick_ActiveProfileHasEntryOverride_BaseEntryTriggerIsNotWatched()
    {
        _harness.Main.EntryTriggerOverride = Trigger(OverrideEntryChord);
        _harness.Start();

        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_EntryOverrideSharedWithBaseTrigger_FirstWatchedWins()
    {
        // Overrides come before the Base Entry Trigger in the watched order.
        _harness.Second.EntryTriggerOverride = Trigger(EntryChord);
        _harness.Start();

        _harness.Hold(0, EntryChord);

        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
    }

    [Fact]
    public void Tick_EntryTriggerCompletedOnTwoSlotsAtOnce_LowerSlotDrives()
    {
        _harness.Source.Connect(1);
        _harness.Start();
        _harness.Source.Set(0, EntryChord);
        _harness.Source.Set(1, EntryChord);

        _harness.Tick();

        Assert.Equal(0, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_ButtonsHeldWhenEntering_AreNotTranslatedUntilReleased()
    {
        _harness.Main.Bindings[ControllerButton.LeftShoulder] = new MouseButtonAction(MouseButton.Left);
        _harness.Start();
        _harness.Hold(0, EntryChord);
        _harness.Hold(0, EntryChord);
        Assert.Empty(_harness.Sink.ButtonEvents);

        _harness.Hold(0, None);
        _harness.Hold(0, ControllerButton.LeftShoulder);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Tick_EntryAndExitTriggersIdentical_StillHoldingAfterEntering_DoesNotExit()
    {
        _harness.Settings.BaseExitTrigger = Trigger(EntryChord);
        _harness.Start();
        _harness.Hold(0, EntryChord);

        _harness.Hold(0, EntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_EntryAndExitTriggersIdentical_ReleasedAndPressedAgain_Exits()
    {
        _harness.Settings.BaseExitTrigger = Trigger(EntryChord);
        _harness.Start();
        _harness.EnterWithBaseTrigger();

        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_EntryAndExitTriggersIdentical_StillHoldingAfterExiting_DoesNotReEnter()
    {
        _harness.Settings.BaseExitTrigger = Trigger(EntryChord);
        _harness.Start();
        // Entered by hand, so the Entry Trigger matcher is not still waiting for its own firing chord's release.
        _harness.Controller.EnterDesktopMode(_harness.Now);
        _harness.Hold(0, EntryChord);
        Assert.False(_harness.Controller.IsDesktopModeActive);

        _harness.Hold(0, EntryChord);
        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void Tick_EntryAndExitTriggersIdentical_ReleasedAfterExitingAndPressedAgain_ReEnters()
    {
        _harness.Settings.BaseExitTrigger = Trigger(EntryChord);
        _harness.Start();
        _harness.EnterWithBaseTrigger();
        _harness.Hold(0, EntryChord);

        _harness.Hold(0, None);
        _harness.Hold(0, EntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }
}
