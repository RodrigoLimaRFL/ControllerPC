using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using static ControllerPC.Core.Tests.DesktopMode.ModeControllerHarness;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class ModeControllerSettingsTests
{
    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton NewEntryChord = ControllerButton.X | ControllerButton.Y;
    private const ControllerButton NewExitChord = ControllerButton.B;

    private readonly ModeControllerHarness _harness = new();

    public ModeControllerSettingsTests()
    {
        _harness.Start();
    }

    [Fact]
    public void ApplySettings_ReplacesSettingsAndRaisesStateChanged()
    {
        var saved = SavedCopy();

        _harness.Controller.ApplySettings(saved, _harness.Now);

        Assert.Same(saved, _harness.Controller.Settings);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void ApplySettings_LiveActiveProfileStillExists_KeepsItActive()
    {
        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);
        var saved = SavedCopy();
        saved.ActiveProfileId = _harness.Main.Id;

        _harness.Controller.ApplySettings(saved, _harness.Now);

        Assert.Equal(_harness.Second.Id, _harness.Controller.ActiveProfile.Id);
        Assert.Equal(_harness.Second.Id, saved.ActiveProfileId);
    }

    [Fact]
    public void ApplySettings_LiveActiveProfileRemoved_UsesTheNewSettingsActiveProfile()
    {
        _harness.Controller.SetActiveProfile(_harness.Second.Id, _harness.Now);
        var saved = SavedCopy();
        saved.Profiles.RemoveAll(profile => profile.Id == _harness.Second.Id);
        saved.ActiveProfileId = _harness.Main.Id;

        _harness.Controller.ApplySettings(saved, _harness.Now);

        Assert.Equal(_harness.Main.Id, _harness.Controller.ActiveProfile.Id);
    }

    [Fact]
    public void ApplySettings_NewBaseEntryTrigger_IsWatched()
    {
        var saved = SavedCopy();
        saved.BaseEntryTrigger = Trigger(NewEntryChord);

        _harness.Controller.ApplySettings(saved, _harness.Now);
        _harness.Hold(0, NewEntryChord);

        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void ApplySettings_OldBaseEntryTrigger_IsNoLongerWatched()
    {
        var saved = SavedCopy();
        saved.BaseEntryTrigger = Trigger(NewEntryChord);

        _harness.Controller.ApplySettings(saved, _harness.Now);
        _harness.Hold(0, EntryChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void ApplySettings_PartOfEntryChordHeldNow_MustBeReleasedBeforeEntering()
    {
        _harness.Hold(0, ControllerButton.LeftShoulder);

        _harness.Controller.ApplySettings(SavedCopy(), _harness.Now);
        _harness.Hold(0, EntryChord);
        Assert.False(_harness.Controller.IsDesktopModeActive);

        _harness.Hold(0, None);
        _harness.Hold(0, EntryChord);
        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void ApplySettings_InDesktopMode_UsesTheNewExitTrigger()
    {
        _harness.EnterWithBaseTrigger();
        var saved = SavedCopy();
        saved.BaseExitTrigger = Trigger(NewExitChord);

        _harness.Controller.ApplySettings(saved, _harness.Now);
        _harness.Hold(0, NewExitChord);

        Assert.False(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void ApplySettings_InDesktopMode_TranslatesWithTheNewBindings()
    {
        _harness.EnterWithBaseTrigger();
        var saved = SavedCopy();
        saved.FindProfile(_harness.Main.Id)!.Bindings[A] = new MouseButtonAction(MouseButton.Forward);

        _harness.Controller.ApplySettings(saved, _harness.Now);
        _harness.Hold(0, A);

        Assert.Equal([MouseEvent("down", MouseButton.Forward)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void ApplySettings_InDesktopMode_ReleasesWhatTheOldBindingsHeld()
    {
        _harness.Main.Bindings[A] = new MouseButtonAction(MouseButton.Left);
        _harness.EnterWithBaseTrigger();
        _harness.Hold(0, A);

        _harness.Controller.ApplySettings(SavedCopy(), _harness.Now);

        Assert.Empty(_harness.Sink.MouseButtonsDown);
        Assert.True(_harness.Controller.IsDesktopModeActive);
    }

    [Fact]
    public void ApplySettings_InDesktopModeWithButtonHeld_IgnoresItUntilReleased()
    {
        _harness.EnterWithBaseTrigger();
        _harness.Hold(0, A);
        var saved = SavedCopy();
        saved.FindProfile(_harness.Main.Id)!.Bindings[A] = new MouseButtonAction(MouseButton.Forward);

        _harness.Controller.ApplySettings(saved, _harness.Now);
        _harness.Hold(0, A);
        Assert.Empty(_harness.Sink.ButtonEvents);

        _harness.Hold(0, None);
        _harness.Hold(0, A);
        Assert.Equal([MouseEvent("down", MouseButton.Forward)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void ApplySettings_NoProfiles_ThrowsArgumentException()
    {
        var saved = SavedCopy();
        saved.Profiles.Clear();

        Assert.Throws<ArgumentException>(() => _harness.Controller.ApplySettings(saved, _harness.Now));
    }

    [Fact]
    public void ApplySettings_NoProfiles_KeepsTheCurrentSettingsInEffect()
    {
        var saved = SavedCopy();
        saved.Profiles.Clear();

        Assert.Throws<ArgumentException>(() => _harness.Controller.ApplySettings(saved, _harness.Now));
        _harness.Hold(0, EntryChord);

        Assert.Same(_harness.Settings, _harness.Controller.Settings);
        Assert.Same(_harness.Main, _harness.Controller.ActiveProfile);
        Assert.True(_harness.Controller.IsDesktopModeActive);
        Assert.Equal(1, _harness.StateChangedCount);
    }

    [Fact]
    public void Constructor_NoProfiles_ThrowsArgumentException()
    {
        _harness.Settings.Profiles.Clear();

        Assert.Throws<ArgumentException>(() => new ModeController(_harness.Source, _harness.Rumble, _harness.Sink, _harness.Settings));
    }

    /// <summary>A copy of the harness settings, as the settings window would save them.</summary>
    private AppSettings SavedCopy() => SettingsSerializer.Clone(_harness.Settings);
}
