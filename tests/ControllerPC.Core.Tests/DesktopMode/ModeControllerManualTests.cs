using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.DesktopMode.ModeControllerHarness;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class ModeControllerManualTests
{
    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton OverrideEntryChord = ControllerButton.X | ControllerButton.Y;

    private readonly ModeControllerHarness _harness = new();

    public ModeControllerManualTests()
    {
        _harness.Main.Bindings[A] = new MouseButtonAction(MouseButton.Left);
        _harness.Second.Bindings[A] = new MouseButtonAction(MouseButton.Right);
    }

    [Fact]
    public void EnterDesktopMode_ControllerConnected_EntersAndReturnsTrue()
    {
        _harness.Start();

        Assert.True(_harness.Controller.EnterDesktopMode(_harness.Now));
        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void EnterDesktopMode_FirstConnectedSlotBecomesTheDrivingController()
    {
        _harness.Source.Disconnect(0);
        _harness.Source.Connect(2);
        _harness.Source.Connect(3);
        _harness.Start();

        _harness.Controller.EnterDesktopMode(_harness.Now);

        Assert.Equal(2, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void EnterDesktopMode_NoControllerConnected_ReturnsFalse()
    {
        _harness.Source.Disconnect(0);
        _harness.Start();

        Assert.False(_harness.Controller.EnterDesktopMode(_harness.Now));
        Assert.False(_harness.Controller.IsDesktopModeActive);
        Assert.Equal(0, _harness.StateChangedCount);
    }

    [Fact]
    public void EnterDesktopMode_AlreadyInDesktopMode_ReturnsFalseAndKeepsDrivingController()
    {
        _harness.Source.Connect(1);
        _harness.Start();
        _harness.EnterWithBaseTrigger(slot: 1);

        Assert.False(_harness.Controller.EnterDesktopMode(_harness.Now));
        Assert.Equal(1, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void EnterDesktopMode_RumblesEnteredAndRaisesStateChanged()
    {
        _harness.Start();

        _harness.Controller.EnterDesktopMode(_harness.Now);

        Assert.Equal([(0, RumblePattern.EnteredDesktopMode)], _harness.Rumble.Played);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void EnterDesktopMode_ButtonsHeldNow_AreNotTranslatedUntilReleased()
    {
        _harness.Start();
        _harness.Hold(0, A);

        _harness.Controller.EnterDesktopMode(_harness.Now);
        _harness.Hold(0, A);
        Assert.Empty(_harness.Sink.ButtonEvents);

        _harness.Hold(0, None);
        _harness.Hold(0, A);
        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void EnterDesktopMode_ExitChordHeldNow_DoesNotExitUntilReleased()
    {
        _harness.Start();
        _harness.Hold(0, ExitChord);

        _harness.Controller.EnterDesktopMode(_harness.Now);
        _harness.Hold(0, ExitChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void EnterDesktopMode_WhilePaused_ReturnsFalse()
    {
        _harness.Start();
        _harness.Controller.Pause();

        Assert.False(_harness.Controller.EnterDesktopMode(_harness.Now));
    }

    [Fact]
    public void ExitDesktopMode_LeavesRumblesAndReleasesEverythingHeld()
    {
        _harness.Start();
        _harness.EnterWithBaseTrigger();
        _harness.Hold(0, A);
        _harness.ClearRecordings();

        _harness.Controller.ExitDesktopMode(_harness.Now);

        Assert.False(_harness.Controller.IsDesktopModeActive);
        Assert.Equal([(0, RumblePattern.ExitedDesktopMode)], _harness.Rumble.Played);
        Assert.Equal([MouseEvent("up", MouseButton.Left)], _harness.Sink.ButtonEvents);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void ExitDesktopMode_EntryChordHeldNow_DoesNotReEnterUntilReleased()
    {
        _harness.Start();
        _harness.Controller.EnterDesktopMode(_harness.Now);
        _harness.Hold(0, EntryChord);

        _harness.Controller.ExitDesktopMode(_harness.Now);
        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void ExitDesktopMode_OutsideDesktopMode_DoesNothing()
    {
        _harness.Start();

        _harness.Controller.ExitDesktopMode(_harness.Now);

        Assert.Empty(_harness.Rumble.Played);
        Assert.Equal(0, _harness.StateChangedCount);
    }

    [Fact]
    public void SetActiveProfile_KnownId_MakesItActiveAndRaisesStateChanged()
    {
        _harness.Start();

        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);

        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
        Assert.Equal(_harness.Second.Id, _harness.Settings.ActiveProfileId);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void SetActiveProfile_UnknownId_IsIgnored()
    {
        _harness.Start();

        _harness.Controller.SetActiveProfile(Guid.NewGuid(), _harness.Now);

        Assert.Same(_harness.Main, _harness.Controller.ActiveProfile);
        Assert.Equal(0, _harness.StateChangedCount);
    }

    [Fact]
    public void SetActiveProfile_ActiveId_DoesNothing()
    {
        _harness.Start();

        _harness.Controller.SetActiveProfile(_harness.Main.Id, _harness.Now);

        Assert.Equal(0, _harness.StateChangedCount);
    }

    [Fact]
    public void SetActiveProfile_OutsideDesktopMode_DoesNotRumble()
    {
        _harness.Start();

        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);

        Assert.Empty(_harness.Rumble.Played);
    }

    [Fact]
    public void SetActiveProfile_ToProfileWithoutEntryOverride_StartsWatchingTheBaseEntryTrigger()
    {
        _harness.Main.EntryTriggerOverride = Trigger(OverrideEntryChord);
        _harness.Start();

        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);
        _harness.Hold(0, EntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
        Assert.Same(_harness.Second, _harness.Controller.ActiveProfile);
    }

    [Fact]
    public void SetActiveProfile_ToProfileWithEntryOverride_StopsWatchingTheBaseEntryTrigger()
    {
        _harness.Second.EntryTriggerOverride = Trigger(OverrideEntryChord);
        _harness.Start();

        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);
        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void SetActiveProfile_InDesktopMode_TranslatesWithTheNewProfile()
    {
        _harness.Start();
        _harness.EnterWithBaseTrigger();

        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);
        _harness.Hold(0, A);

        Assert.Equal([MouseEvent("down", MouseButton.Right)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void SetActiveProfile_InDesktopMode_RumblesProfileSwitched()
    {
        _harness.Start();
        _harness.EnterWithBaseTrigger();
        _harness.ClearRecordings();

        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);

        Assert.Equal([(0, RumblePattern.ProfileSwitched)], _harness.Rumble.Played);
    }
}
