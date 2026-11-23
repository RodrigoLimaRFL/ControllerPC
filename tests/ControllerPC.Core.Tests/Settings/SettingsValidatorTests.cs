using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Tests.Settings;

public class SettingsValidatorTests
{
    private readonly AppSettings _settings = DefaultSettings.Create();

    private Profile DefaultProfile => _settings.Profiles[0];

    // Profiles

    [Fact]
    public void Validate_NoProfiles_IsError()
    {
        _settings.Profiles.Clear();

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Fact]
    public void Validate_DuplicateProfileIds_IsError()
    {
        var copy = TestSettings.AddProfile(_settings, "Copy");
        copy.Id = DefaultProfile.Id;

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_DistinctProfileIdsAndNames_HasNoIssues()
    {
        TestSettings.AddProfile(_settings, "Work");

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankProfileName_IsError(string name)
    {
        DefaultProfile.Name = name;

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("DEFAULT")]
    [InlineData(" default ")]
    public void Validate_DuplicateProfileNameIgnoringCase_IsErrorOnLaterProfile(string name)
    {
        var duplicate = TestSettings.AddProfile(_settings, name);

        AssertSingleIssue(IssueSeverity.Error, duplicate.Id);
    }

    // Mode Trigger shape and timing

    [Fact]
    public void Validate_BaseEntryTriggerWithNoSteps_IsError()
    {
        _settings.BaseEntryTrigger.Steps.Clear();

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Fact]
    public void Validate_BaseExitTriggerWithNoSteps_IsError()
    {
        _settings.BaseExitTrigger.Steps.Clear();

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Fact]
    public void Validate_EntryTriggerOverrideWithNoSteps_IsErrorForItsProfile()
    {
        DefaultProfile.EntryTriggerOverride = TestSettings.Trigger();

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ExitTriggerOverrideWithNoSteps_IsErrorForItsProfile()
    {
        DefaultProfile.ExitTriggerOverride = TestSettings.Trigger();

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_StepWithEmptyChord_IsError()
    {
        _settings.BaseExitTrigger.Steps.Add(new Chord(ControllerButton.None));

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Fact]
    public void Validate_OverrideStepWithEmptyChord_IsErrorForItsProfile()
    {
        DefaultProfile.ExitTriggerOverride = TestSettings.Trigger(ControllerButton.Back, ControllerButton.None);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_StepGapNotPositive_IsError(int milliseconds)
    {
        _settings.BaseEntryTrigger.StepGap = TimeSpan.FromMilliseconds(milliseconds);

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_FinalHoldDurationNotPositive_IsError(int milliseconds)
    {
        _settings.BaseExitTrigger.FinalHoldDuration = TimeSpan.FromMilliseconds(milliseconds);

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Fact]
    public void Validate_FinalHoldDurationNotPositiveWhileDisabled_IsStillError()
    {
        _settings.BaseExitTrigger.FinalHoldEnabled = false;
        _settings.BaseExitTrigger.FinalHoldDuration = TimeSpan.Zero;

        AssertSingleIssue(IssueSeverity.Error, profileId: null);
    }

    [Fact]
    public void Validate_PositiveTimingOnOverride_HasNoIssues()
    {
        var trigger = TestSettings.Trigger(ControllerButton.Back, ControllerButton.Start);
        trigger.StepGap = TimeSpan.FromMilliseconds(1);
        trigger.FinalHoldDuration = TimeSpan.FromMilliseconds(1);
        DefaultProfile.ExitTriggerOverride = trigger;

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    // Entry Trigger conflicts

    [Fact]
    public void Validate_EntryTriggerOverrideEqualToBaseEntryTrigger_IsError()
    {
        var other = TestSettings.AddProfile(_settings, "Other");
        other.EntryTriggerOverride = TestSettings.Trigger(TestSettings.DefaultEntryChord);

        AssertSingleIssue(IssueSeverity.Error, other.Id);
    }

    [Fact]
    public void Validate_EntryTriggerOverrideEqualToBaseOnActiveProfile_IsStillError()
    {
        DefaultProfile.EntryTriggerOverride = TestSettings.Trigger(TestSettings.DefaultEntryChord);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_EntryTriggerOverrideWithSameStepsButDifferentTiming_IsError()
    {
        var other = TestSettings.AddProfile(_settings, "Other");
        other.EntryTriggerOverride = TestSettings.Trigger(TestSettings.DefaultEntryChord);
        other.EntryTriggerOverride.FinalHoldEnabled = false;
        other.EntryTriggerOverride.StepGap = TimeSpan.FromSeconds(3);

        AssertSingleIssue(IssueSeverity.Error, other.Id);
    }

    [Fact]
    public void Validate_TwoEqualEntryTriggerOverrides_IsErrorOnLaterProfile()
    {
        var first = TestSettings.AddProfile(_settings, "First");
        var second = TestSettings.AddProfile(_settings, "Second");
        first.EntryTriggerOverride = TestSettings.Trigger(ControllerButton.Back | ControllerButton.X);
        second.EntryTriggerOverride = TestSettings.Trigger(ControllerButton.Back | ControllerButton.X);

        AssertSingleIssue(IssueSeverity.Error, second.Id);
    }

    [Fact]
    public void Validate_DistinctEntryTriggerOverrides_HasNoIssues()
    {
        TestSettings.AddProfile(_settings, "First").EntryTriggerOverride =
            TestSettings.Trigger(ControllerButton.Back | ControllerButton.X);
        TestSettings.AddProfile(_settings, "Second").EntryTriggerOverride =
            TestSettings.Trigger(ControllerButton.Back | ControllerButton.Y);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_EntryTriggerOverrideEqualToExitTrigger_HasNoIssues()
    {
        var trigger = TestSettings.Trigger(ControllerButton.Back | ControllerButton.X);
        DefaultProfile.EntryTriggerOverride = trigger;
        DefaultProfile.ExitTriggerOverride = TestSettings.Trigger(ControllerButton.Back | ControllerButton.X);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    // Bindings

    [Fact]
    public void Validate_BindingForNoButton_IsError()
    {
        DefaultProfile.Bindings[ControllerButton.None] = new MouseButtonAction(MouseButton.Middle);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_BindingForSeveralButtons_IsError()
    {
        DefaultProfile.Bindings[ControllerButton.X | ControllerButton.Back] = new MouseButtonAction(MouseButton.Middle);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_BindingForEverySingleButton_HasNoIssues()
    {
        foreach (var button in ControllerButtons.All)
        {
            DefaultProfile.Bindings[button] = new NudgeAction(NudgeDirection.Up);
        }

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_KeyCombinationWithNoKeys_IsError()
    {
        DefaultProfile.Bindings[ControllerButton.X] = new KeyCombinationAction(new KeyCombination([]));

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    // Tuning

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void Validate_DeadzoneOutsideRange_IsError(double deadzone)
    {
        DefaultProfile.Tuning.Deadzone = deadzone;

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.99)]
    public void Validate_DeadzoneInsideRange_HasNoIssues(double deadzone)
    {
        DefaultProfile.Tuning.Deadzone = deadzone;

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Validate_CursorSpeedNotPositive_IsError(double speed)
    {
        DefaultProfile.Tuning.CursorSpeed = speed;

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Validate_ScrollSpeedNotPositive_IsError(double speed)
    {
        DefaultProfile.Tuning.ScrollSpeed = speed;

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_SmallPositiveSpeeds_HasNoIssues()
    {
        DefaultProfile.Tuning.CursorSpeed = 0.01;
        DefaultProfile.Tuning.ScrollSpeed = 0.01;

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NudgeStepBelowOne_IsError(int nudgeStep)
    {
        DefaultProfile.Tuning.NudgeStep = nudgeStep;

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    // Warnings

    [Fact]
    public void Validate_EntryTriggerOverrideStartingWithBaseEntryTrigger_WarnsOnOverride()
    {
        var other = TestSettings.AddProfile(_settings, "Other");
        other.EntryTriggerOverride = TestSettings.Trigger(TestSettings.DefaultEntryChord, ControllerButton.A);

        AssertSingleIssue(IssueSeverity.Warning, other.Id);
    }

    [Fact]
    public void Validate_BaseEntryTriggerStartingWithEntryTriggerOverride_WarnsOnBase()
    {
        var other = TestSettings.AddProfile(_settings, "Other");
        other.EntryTriggerOverride = TestSettings.Trigger(ControllerButton.Back);
        _settings.BaseEntryTrigger.Steps = [new Chord(ControllerButton.Back), new Chord(ControllerButton.Start)];

        AssertSingleIssue(IssueSeverity.Warning, profileId: null);
    }

    [Fact]
    public void Validate_EntryTriggerOverrideStartingWithAnotherOverride_WarnsOnLongerOne()
    {
        var shorter = TestSettings.AddProfile(_settings, "Shorter");
        var longer = TestSettings.AddProfile(_settings, "Longer");
        shorter.EntryTriggerOverride = TestSettings.Trigger(ControllerButton.Back);
        longer.EntryTriggerOverride = TestSettings.Trigger(ControllerButton.Back, ControllerButton.X);

        AssertSingleIssue(IssueSeverity.Warning, longer.Id);
    }

    [Fact]
    public void Validate_EntryTriggersSharingOnlyFirstStep_HasNoIssues()
    {
        TestSettings.AddProfile(_settings, "First").EntryTriggerOverride =
            TestSettings.Trigger(ControllerButton.Back, ControllerButton.X);
        TestSettings.AddProfile(_settings, "Second").EntryTriggerOverride =
            TestSettings.Trigger(ControllerButton.Back, ControllerButton.Y);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_ExitTriggerButtonBoundToKeyCombination_Warns()
    {
        DefaultProfile.Bindings[ControllerButton.Start] = new KeyCombinationAction(new KeyCombination([0x0D]));

        AssertSingleIssue(IssueSeverity.Warning, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ExitTriggerOverrideButtonBoundToKeyCombination_Warns()
    {
        DefaultProfile.ExitTriggerOverride = TestSettings.Trigger(ControllerButton.Back, ControllerButton.A);

        AssertSingleIssue(IssueSeverity.Warning, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ExitTriggerButtonBoundToMouseButton_Warns()
    {
        DefaultProfile.Bindings[ControllerButton.Back] = new MouseButtonAction(MouseButton.Middle);

        AssertSingleIssue(IssueSeverity.Warning, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ExitTriggerButtonBoundToNudge_HasNoIssues()
    {
        DefaultProfile.Bindings[ControllerButton.Back] = new NudgeAction(NudgeDirection.Up);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_ExitTriggerHeldInputWarning_NamesOverlappingButtons()
    {
        DefaultProfile.Bindings[ControllerButton.Back] = new MouseButtonAction(MouseButton.Middle);
        DefaultProfile.Bindings[ControllerButton.Start] = new KeyCombinationAction(new KeyCombination([0x0D]));

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Contains("uses Start, Back,", issue.Message);
    }

    [Fact]
    public void Validate_EntryTriggerPrefixWarning_SaysShorterMayFireFirst()
    {
        var other = TestSettings.AddProfile(_settings, "Racing");
        other.EntryTriggerOverride = TestSettings.Trigger(TestSettings.DefaultEntryChord, ControllerButton.A);

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Equal(
            "The Entry Trigger Override of Profile \"Racing\" starts with all the Steps of the Base Entry Trigger, " +
            "so the Base Entry Trigger may fire first and prevent the Entry Trigger Override of Profile \"Racing\" " +
            "from firing.",
            issue.Message);
    }

    [Fact]
    public void Validate_SwitchToMissingProfile_Warns()
    {
        DefaultProfile.Bindings[ControllerButton.X] = new SwitchToProfileAction(Guid.NewGuid());

        AssertSingleIssue(IssueSeverity.Warning, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_SwitchToExistingProfile_HasNoIssues()
    {
        var target = TestSettings.AddProfile(_settings, "Target");
        DefaultProfile.Bindings[ControllerButton.X] = new SwitchToProfileAction(target.Id);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    // Chord Bindings

    [Fact]
    public void Validate_ChordBindingWithTwoButtons_HasNoIssues()
    {
        AddChordBinding(ControllerButton.A | ControllerButton.X, new MouseButtonAction(MouseButton.Middle));

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Theory]
    [InlineData(ControllerButton.None)]
    [InlineData(ControllerButton.A)]
    public void Validate_ChordBindingWithFewerThanTwoButtons_IsError(ControllerButton chord)
    {
        AddChordBinding(chord, NoAction.Instance);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_TwoChordBindingsWithTheSameChord_IsError()
    {
        AddChordBinding(ControllerButton.A | ControllerButton.X, NoAction.Instance);
        AddChordBinding(ControllerButton.X | ControllerButton.A, new NextProfileAction());

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_SameChordBindingInTwoProfiles_HasNoIssues()
    {
        var other = TestSettings.AddProfile(_settings, "Racing");
        AddChordBinding(ControllerButton.A | ControllerButton.X, NoAction.Instance);
        other.ChordBindings.Add(new ChordBinding { Chord = new Chord(ControllerButton.A | ControllerButton.X) });

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_ChordBindingWithinTheBaseExitTriggerChord_IsError()
    {
        AddChordBinding(TestSettings.DefaultExitChord, NoAction.Instance);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ChordBindingWithinAnExitTriggerOverrideStep_IsError()
    {
        DefaultProfile.ExitTriggerOverride = TestSettings.Trigger(
            ControllerButton.LeftThumb | ControllerButton.RightThumb,
            ControllerButton.X | ControllerButton.DPadUp | ControllerButton.DPadDown);
        AddChordBinding(ControllerButton.X | ControllerButton.DPadDown, NoAction.Instance);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ChordBindingWithinTheBaseExitTriggerOfProfileWithExitOverride_HasNoIssues()
    {
        DefaultProfile.ExitTriggerOverride = TestSettings.Trigger(ControllerButton.LeftThumb | ControllerButton.RightThumb);
        AddChordBinding(TestSettings.DefaultExitChord, NoAction.Instance);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_ChordBindingSharingOnlySomeExitTriggerButtons_HasNoIssues()
    {
        AddChordBinding(ControllerButton.Back | ControllerButton.X, NoAction.Instance);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_ChordBindingWithinExitTrigger_SaysItWouldBePerformedOnEveryExit()
    {
        AddChordBinding(TestSettings.DefaultExitChord, NoAction.Instance);

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Equal(
            "The Chord Binding Start+Back in Profile \"Default\" only uses buttons of a Step of the Profile's " +
            "Effective Exit Trigger, so it would be performed every time Desktop Mode is exited.",
            issue.Message);
    }

    [Fact]
    public void Validate_ChordBindingKeyCombinationWithNoKeys_IsError()
    {
        AddChordBinding(ControllerButton.A | ControllerButton.X, new KeyCombinationAction(new KeyCombination([])));

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ChordBindingSwitchToMissingProfile_Warns()
    {
        AddChordBinding(ControllerButton.A | ControllerButton.X, new SwitchToProfileAction(Guid.NewGuid()));

        AssertSingleIssue(IssueSeverity.Warning, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_ChordBindingSwitchToExistingProfile_HasNoIssues()
    {
        var target = TestSettings.AddProfile(_settings, "Target");
        AddChordBinding(ControllerButton.A | ControllerButton.X, new SwitchToProfileAction(target.Id));

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(501)]
    public void Validate_ChordWaitOutsideRange_IsError(int milliseconds)
    {
        DefaultProfile.ChordWait = TimeSpan.FromMilliseconds(milliseconds);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void Validate_ChordWaitInsideRange_HasNoIssues(int milliseconds)
    {
        DefaultProfile.ChordWait = TimeSpan.FromMilliseconds(milliseconds);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    // Macros

    [Fact]
    public void Validate_MacroWithKeyCombinations_HasNoIssues()
    {
        DefaultProfile.Bindings[ControllerButton.X] = Macro(MacroAction.DefaultDelay, [0x11, 0x43], [0x0D]);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_MacroWithNoKeyCombinations_IsError()
    {
        DefaultProfile.Bindings[ControllerButton.X] = Macro(MacroAction.DefaultDelay);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_MacroWithAKeyCombinationWithNoKeys_IsError()
    {
        DefaultProfile.Bindings[ControllerButton.X] = Macro(MacroAction.DefaultDelay, [0x0D], []);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2001)]
    public void Validate_MacroDelayOutsideRange_IsError(int milliseconds)
    {
        DefaultProfile.Bindings[ControllerButton.X] = Macro(TimeSpan.FromMilliseconds(milliseconds), [0x0D]);

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2000)]
    public void Validate_MacroDelayInsideRange_HasNoIssues(int milliseconds)
    {
        DefaultProfile.Bindings[ControllerButton.X] = Macro(TimeSpan.FromMilliseconds(milliseconds), [0x0D]);

        Assert.Empty(SettingsValidator.Validate(_settings));
    }

    [Fact]
    public void Validate_ChordBindingMacroWithNoKeyCombinations_IsError()
    {
        AddChordBinding(ControllerButton.A | ControllerButton.X, Macro(MacroAction.DefaultDelay));

        AssertSingleIssue(IssueSeverity.Error, DefaultProfile.Id);
    }

    [Fact]
    public void Validate_MacroWithNoKeyCombinations_SaysSo()
    {
        DefaultProfile.Bindings[ControllerButton.X] = Macro(MacroAction.DefaultDelay);

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Equal("The Binding for X in Profile \"Default\" has a Macro with no Key Combinations.", issue.Message);
    }

    [Fact]
    public void Validate_ExitTriggerButtonBoundToMacro_Warns()
    {
        DefaultProfile.Bindings[ControllerButton.Back] = Macro(MacroAction.DefaultDelay, [0x0D]);

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.Contains("binds to Key Combinations, Macros or mouse buttons", issue.Message);
    }

    // Messages

    [Fact]
    public void Validate_ProfileIssue_NamesTheProfileInMessage()
    {
        var other = TestSettings.AddProfile(_settings, "Racing");
        other.Tuning.NudgeStep = 0;

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Contains("Profile \"Racing\"", issue.Message);
    }

    [Fact]
    public void Validate_EntryTriggerConflict_NamesBothTriggersInMessage()
    {
        var other = TestSettings.AddProfile(_settings, "Racing");
        other.EntryTriggerOverride = TestSettings.Trigger(TestSettings.DefaultEntryChord);

        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Equal(
            "The Entry Trigger Override of Profile \"Racing\" has the same Steps as the Base Entry Trigger.",
            issue.Message);
    }

    private static MacroAction Macro(TimeSpan delay, params ushort[][] keyCombinations) =>
        new(keyCombinations.Select(keys => new KeyCombination(keys)), delay);

    private void AddChordBinding(ControllerButton chord, ButtonAction action) =>
        DefaultProfile.ChordBindings.Add(new ChordBinding { Chord = new Chord(chord), Action = action });

    private void AssertSingleIssue(IssueSeverity severity, Guid? profileId)
    {
        var issue = Assert.Single(SettingsValidator.Validate(_settings));

        Assert.Equal(severity, issue.Severity);
        Assert.Equal(profileId, issue.ProfileId);
        Assert.False(string.IsNullOrWhiteSpace(issue.Message));
    }
}
