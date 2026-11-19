using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Tests.Settings;

public class DefaultSettingsTests
{
    [Fact]
    public void Create_PassesValidationWithNoIssues()
    {
        Assert.Empty(SettingsValidator.Validate(DefaultSettings.Create()));
    }

    [Fact]
    public void Create_HasOneDefaultProfileThatIsActive()
    {
        var settings = DefaultSettings.Create();

        var profile = Assert.Single(settings.Profiles);
        Assert.Equal("Default", profile.Name);
        Assert.Equal(profile.Id, settings.ActiveProfileId);
    }

    [Fact]
    public void Create_UsesCurrentVersion()
    {
        Assert.Equal(AppSettings.CurrentVersion, DefaultSettings.Create().Version);
    }

    [Fact]
    public void Create_BaseEntryTriggerIsShouldersPlusStartWithFinalHold()
    {
        AssertShouldersPlusStartWithFinalHold(DefaultSettings.Create().BaseEntryTrigger);
    }

    [Fact]
    public void Create_BaseExitTriggerIsShouldersPlusStartWithFinalHold()
    {
        AssertShouldersPlusStartWithFinalHold(DefaultSettings.Create().BaseExitTrigger);
    }

    [Fact]
    public void Create_ProfileHasNoTriggerOverrides()
    {
        var profile = DefaultSettings.Create().ActiveProfile;

        Assert.Null(profile.EntryTriggerOverride);
        Assert.Null(profile.ExitTriggerOverride);
    }

    [Fact]
    public void Create_ProfileScrollsWithRightStickAndUsesDefaultTuning()
    {
        var profile = DefaultSettings.Create().ActiveProfile;

        Assert.Equal(RightStickRole.Scroll, profile.RightStickRole);
        Assert.Equal(MovementTuning.DefaultDeadzone, profile.Tuning.Deadzone);
        Assert.Equal(MovementTuning.DefaultCursorSpeed, profile.Tuning.CursorSpeed);
        Assert.Equal(MovementTuning.DefaultNudgeStep, profile.Tuning.NudgeStep);
        Assert.Equal(MovementTuning.DefaultScrollSpeed, profile.Tuning.ScrollSpeed);
    }

    [Fact]
    public void Create_ProfileHasDefaultBindingsAndNothingElse()
    {
        var expected = new Dictionary<ControllerButton, ButtonAction>
        {
            [ControllerButton.LeftShoulder] = new MouseButtonAction(MouseButton.Left),
            [ControllerButton.RightShoulder] = new MouseButtonAction(MouseButton.Right),
            [ControllerButton.DPadUp] = new NudgeAction(NudgeDirection.Up),
            [ControllerButton.DPadDown] = new NudgeAction(NudgeDirection.Down),
            [ControllerButton.DPadLeft] = new NudgeAction(NudgeDirection.Left),
            [ControllerButton.DPadRight] = new NudgeAction(NudgeDirection.Right),
            [ControllerButton.A] = new KeyCombinationAction(new KeyCombination([0x0D])),
            [ControllerButton.B] = new KeyCombinationAction(new KeyCombination([0x1B])),
            [ControllerButton.Y] = new KeyCombinationAction(new KeyCombination([0x5B, 0x11, 0x4F])),
        };

        Assert.Equal(expected, DefaultSettings.Create().ActiveProfile.Bindings);
    }

    [Fact]
    public void Create_ReturnsIndependentSettingsEachTime()
    {
        var first = DefaultSettings.Create();
        var second = DefaultSettings.Create();

        first.ActiveProfile.Bindings.Clear();
        first.BaseEntryTrigger.Steps.Clear();

        Assert.NotEmpty(second.ActiveProfile.Bindings);
        Assert.NotEmpty(second.BaseEntryTrigger.Steps);
    }

    private static void AssertShouldersPlusStartWithFinalHold(ModeTrigger trigger)
    {
        Assert.Equal([new Chord(TestSettings.DefaultTriggerChord)], trigger.Steps);
        Assert.True(trigger.FinalHoldEnabled);
        Assert.Equal(ModeTrigger.DefaultFinalHoldDuration, trigger.FinalHoldDuration);
        Assert.Equal(ModeTrigger.DefaultStepGap, trigger.StepGap);
    }
}
