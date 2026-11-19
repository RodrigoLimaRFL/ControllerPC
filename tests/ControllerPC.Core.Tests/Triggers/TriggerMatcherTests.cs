using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;
using static ControllerPC.Core.Controller.ControllerButton;

namespace ControllerPC.Core.Tests.Triggers;

public class TriggerMatcherTests
{
    private static readonly TimeSpan StepGap = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan FinalHold = TimeSpan.FromMilliseconds(500);

    // Single-Step triggers

    [Fact]
    public void Update_FiresWhenSingleChordIsHeldExactly()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        Assert.True(matcher.Update(Back | Start, Ms(0)));
    }

    [Fact]
    public void Update_DoesNotFireWhileChordIsOnlyPartiallyHeld()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        Assert.False(matcher.Update(Back, Ms(0)));
        Assert.False(matcher.Update(Back, Ms(5000)));
    }

    [Fact]
    public void Update_FiresWhenChordIsBuiltUpOneButtonAtATime()
    {
        var matcher = new TriggerMatcher(TriggerOf(LeftShoulder | RightShoulder | A));

        matcher.Update(LeftShoulder, Ms(0));
        matcher.Update(LeftShoulder | RightShoulder, Ms(10));

        Assert.True(matcher.Update(LeftShoulder | RightShoulder | A, Ms(20)));
    }

    [Fact]
    public void Update_PressOrderWithinChordDoesNotMatter()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        matcher.Update(Start, Ms(0));

        Assert.True(matcher.Update(Start | Back, Ms(10)));
    }

    [Fact]
    public void Update_ReleasingPartOfChordWhileBuildingUpIsNotABreak()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        matcher.Update(Back, Ms(0));
        matcher.Update(None, Ms(10));
        matcher.Update(Start, Ms(20));

        Assert.True(matcher.Update(Start | Back, Ms(30)));
    }

    // Buttons outside the expected Chord

    [Fact]
    public void Update_ButtonOutsideChordResetsAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        matcher.Update(Back, Ms(0));
        matcher.Update(Back | A, Ms(10));

        Assert.False(matcher.Update(Back | Start, Ms(20)));
    }

    [Fact]
    public void Update_ExpectedChordPlusExtraButtonDoesNotFire()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        Assert.False(matcher.Update(Back | Start | A, Ms(0)));
    }

    [Fact]
    public void Update_AfterBreakButtonsStillHeldBlockNewAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        matcher.Update(Back | A, Ms(0));
        matcher.Update(Back, Ms(10));

        Assert.False(matcher.Update(Back | Start, Ms(20)));
    }

    [Fact]
    public void Update_AfterBreakNewAttemptStartsOnceEverythingIsReleased()
    {
        var matcher = new TriggerMatcher(TriggerOf(Back | Start));

        matcher.Update(Back | A, Ms(0));
        matcher.Update(None, Ms(10));

        Assert.True(matcher.Update(Back | Start, Ms(20)));
    }

    // Sequences

    [Fact]
    public void Update_PureSequenceFiresWhenEachStepIsPressedAndReleasedInOrder()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B, X));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(B, Ms(200));
        matcher.Update(None, Ms(300));

        Assert.True(matcher.Update(X, Ms(400)));
    }

    [Fact]
    public void Update_RepeatedStepFiresWhenReleasedBetweenPresses()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, A));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));

        Assert.True(matcher.Update(A, Ms(200)));
    }

    [Fact]
    public void Update_RepeatedStepDoesNotAdvanceWhileChordStaysHeld()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, A));

        Assert.False(matcher.Update(A, Ms(0)));
        Assert.False(matcher.Update(A, Ms(100)));
        Assert.False(matcher.Update(A, Ms(200)));
    }

    [Fact]
    public void Update_PressingNextStepBeforeReleasingPreviousResetsAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(A, Ms(0));
        matcher.Update(A | B, Ms(100));

        Assert.False(matcher.Update(B, Ms(200)));
    }

    [Fact]
    public void Update_PartlyReleasingCompletedStepDoesNotStartNextStep()
    {
        var matcher = new TriggerMatcher(TriggerOf(A | B, X));

        matcher.Update(A | B, Ms(0));
        matcher.Update(A, Ms(100));

        Assert.False(matcher.Update(A | X, Ms(200)));
    }

    [Fact]
    public void Update_StepsCompletedOutOfOrderDoNotFire()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(B, Ms(0));
        matcher.Update(None, Ms(100));

        Assert.False(matcher.Update(A, Ms(200)));
    }

    [Fact]
    public void Update_MixedSequenceOfChordsAndSingleButtonsFires()
    {
        var matcher = new TriggerMatcher(TriggerOf(LeftShoulder | RightShoulder, A, B | X));

        matcher.Update(RightShoulder, Ms(0));
        matcher.Update(RightShoulder | LeftShoulder, Ms(50));
        matcher.Update(None, Ms(100));
        matcher.Update(A, Ms(200));
        matcher.Update(None, Ms(300));
        matcher.Update(X, Ms(400));

        Assert.True(matcher.Update(X | B, Ms(450)));
    }

    // Step Gap

    [Fact]
    public void Update_FirstStepHasNoTimeLimit()
    {
        var matcher = new TriggerMatcher(TriggerOf(A | B, X));

        matcher.Update(None, Ms(0));
        matcher.Update(A, Ms(60_000));
        matcher.Update(A | B, Ms(120_000));
        matcher.Update(None, Ms(120_100));

        Assert.True(matcher.Update(X, Ms(120_200)));
    }

    [Fact]
    public void Update_FiresWhenNextStepCompletesExactlyAtStepGap()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));

        Assert.True(matcher.Update(B, Ms(100) + StepGap));
    }

    [Fact]
    public void Update_ResetsWhenStepGapExpiresBeforeNextStep()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(None, Ms(101) + StepGap);

        Assert.False(matcher.Update(B, Ms(102) + StepGap));
    }

    [Fact]
    public void Update_AfterStepGapExpiresFirstStepStartsNewAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(None, Ms(101) + StepGap);
        matcher.Update(A, Ms(5000));
        matcher.Update(None, Ms(5100));

        Assert.True(matcher.Update(B, Ms(5200)));
    }

    [Fact]
    public void Update_StepGapIsMeasuredFromReleaseOfPreviousStep()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(A, Ms(0));
        matcher.Update(A, Ms(5000));
        matcher.Update(None, Ms(5000));

        Assert.True(matcher.Update(B, Ms(5000) + StepGap));
    }

    [Fact]
    public void Update_StepGapRunsUntilNextChordIsCompletedNotJustStarted()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B | X));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(B, Ms(200));

        Assert.False(matcher.Update(B | X, Ms(101) + StepGap));
    }

    [Fact]
    public void Update_UsesStepGapOfTheTrigger()
    {
        var trigger = TriggerOf(A, B);
        trigger.StepGap = TimeSpan.FromMilliseconds(200);
        var matcher = new TriggerMatcher(trigger);

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));

        Assert.False(matcher.Update(B, Ms(301)));
    }

    // Final Hold

    [Fact]
    public void Update_WithFinalHoldDoesNotFireBeforeDuration()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(Back | Start)));

        Assert.False(matcher.Update(Back | Start, Ms(0)));
        Assert.False(matcher.Update(Back | Start, Ms(0) + FinalHold - Ms(1)));
    }

    [Fact]
    public void Update_FiresWhenFinalChordHeldForFinalHold()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(Back | Start)));

        matcher.Update(Back | Start, Ms(100));

        Assert.True(matcher.Update(Back | Start, Ms(100) + FinalHold));
    }

    [Fact]
    public void Update_FinalHoldStartsWhenFinalChordBecomesExactlyHeld()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(Back | Start)));

        matcher.Update(Back, Ms(0));
        matcher.Update(Back | Start, Ms(400));

        Assert.False(matcher.Update(Back | Start, Ms(0) + FinalHold));
    }

    [Fact]
    public void Update_ReleasingButtonDuringFinalHoldCancels()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(Back | Start)));

        matcher.Update(Back | Start, Ms(0));
        matcher.Update(Back, Ms(100));

        Assert.False(matcher.Update(Back | Start, Ms(200) + FinalHold));
    }

    [Fact]
    public void Update_AddingButtonDuringFinalHoldCancels()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(Back | Start)));

        matcher.Update(Back | Start, Ms(0));
        matcher.Update(Back | Start | A, Ms(100));

        Assert.False(matcher.Update(Back | Start, Ms(200) + FinalHold));
    }

    [Fact]
    public void Update_CancelledFinalHoldRestartsWholeAttempt()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(A, B)));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(B, Ms(200));
        matcher.Update(None, Ms(300));
        matcher.Update(B, Ms(400));

        Assert.False(matcher.Update(B, Ms(400) + FinalHold));
    }

    [Fact]
    public void Update_FinalHoldCanBeRetriedAfterFullRelease()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(Back | Start)));

        matcher.Update(Back | Start, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(Back | Start, Ms(200));

        Assert.True(matcher.Update(Back | Start, Ms(200) + FinalHold));
    }

    [Fact]
    public void Update_FinalHoldTimeDoesNotCountAgainstStepGap()
    {
        var trigger = WithFinalHold(TriggerOf(A, B));
        trigger.FinalHoldDuration = StepGap * 2;
        var matcher = new TriggerMatcher(trigger);

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Update(B, Ms(200));

        Assert.True(matcher.Update(B, Ms(200) + StepGap * 2));
    }

    [Fact]
    public void Update_FinalHoldAppliesOnlyToLastStep()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(A, B)));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(10));
        matcher.Update(B, Ms(20));

        Assert.True(matcher.Update(B, Ms(20) + FinalHold));
    }

    // Held at start and Reset

    [Fact]
    public void Constructor_ButtonsHeldAtStartDoNotCompleteChord()
    {
        var matcher = new TriggerMatcher(TriggerOf(A), heldAtStart: A);

        Assert.False(matcher.Update(A, Ms(0)));
    }

    [Fact]
    public void Constructor_ButtonsHeldAtStartContributeAfterRelease()
    {
        var matcher = new TriggerMatcher(TriggerOf(A), heldAtStart: A);

        matcher.Update(None, Ms(0));

        Assert.True(matcher.Update(A, Ms(10)));
    }

    [Fact]
    public void Constructor_UnrelatedButtonHeldAtStartBlocksAttemptUntilReleased()
    {
        var matcher = new TriggerMatcher(TriggerOf(A), heldAtStart: X);

        Assert.False(matcher.Update(X | A, Ms(0)));
        Assert.False(matcher.Update(A, Ms(10)));
    }

    [Fact]
    public void Reset_AbandonsInProgressAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(A, B));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(100));
        matcher.Reset(None);

        Assert.False(matcher.Update(B, Ms(200)));
    }

    [Fact]
    public void Reset_HeldButtonsMustBeReleasedBeforeContributing()
    {
        var matcher = new TriggerMatcher(TriggerOf(A));

        matcher.Reset(A);

        Assert.False(matcher.Update(A, Ms(0)));
    }

    [Fact]
    public void Reset_WithNothingHeldAllowsImmediateNewAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(A));

        matcher.Update(X, Ms(0));
        matcher.Reset(None);

        Assert.True(matcher.Update(A, Ms(10)));
    }

    // Firing again

    [Fact]
    public void Update_DoesNotFireAgainWhileChordStaysHeld()
    {
        var matcher = new TriggerMatcher(TriggerOf(A));

        matcher.Update(A, Ms(0));

        Assert.False(matcher.Update(A, Ms(10)));
    }

    [Fact]
    public void Update_WithFinalHoldDoesNotFireAgainWhileChordStaysHeld()
    {
        var matcher = new TriggerMatcher(WithFinalHold(TriggerOf(A)));

        matcher.Update(A, Ms(0));
        matcher.Update(A, Ms(0) + FinalHold);

        Assert.False(matcher.Update(A, Ms(0) + FinalHold * 3));
    }

    [Fact]
    public void Update_FiresAgainAfterReleaseAndRepeat()
    {
        var matcher = new TriggerMatcher(TriggerOf(A));

        matcher.Update(A, Ms(0));
        matcher.Update(None, Ms(10));

        Assert.True(matcher.Update(A, Ms(20)));
    }

    [Fact]
    public void Update_AfterFiringButtonsStillHeldCannotStartNextAttempt()
    {
        var matcher = new TriggerMatcher(TriggerOf(A | B));

        matcher.Update(A | B, Ms(0));
        matcher.Update(A, Ms(10));

        Assert.False(matcher.Update(A | B, Ms(20)));
    }

    // Invalid triggers

    [Fact]
    public void Update_TriggerWithNoStepsNeverFires()
    {
        var matcher = new TriggerMatcher(new ModeTrigger());

        Assert.False(matcher.Update(None, Ms(0)));
        Assert.False(matcher.Update(A, Ms(10)));
        Assert.False(matcher.Update(None, Ms(20)));
    }

    [Fact]
    public void Update_TriggerWithEmptyChordNeverFires()
    {
        var matcher = new TriggerMatcher(TriggerOf(None));

        Assert.False(matcher.Update(None, Ms(0)));
        Assert.False(matcher.Update(None, Ms(10)));
    }

    [Fact]
    public void Constructor_RejectsNullTrigger()
    {
        Assert.Throws<ArgumentNullException>(() => new TriggerMatcher(null!));
    }

    [Fact]
    public void Constructor_CapturesStepsSoLaterEditsDoNotAffectMatcher()
    {
        var trigger = TriggerOf(A);
        var matcher = new TriggerMatcher(trigger);

        trigger.Steps[0] = new Chord(B);

        Assert.True(matcher.Update(A, Ms(0)));
    }

    private static ModeTrigger TriggerOf(params ControllerButton[] steps) => new()
    {
        Steps = steps.Select(buttons => new Chord(buttons)).ToList(),
        StepGap = StepGap,
        FinalHoldDuration = FinalHold,
    };

    private static ModeTrigger WithFinalHold(ModeTrigger trigger)
    {
        trigger.FinalHoldEnabled = true;
        return trigger;
    }

    private static TimeSpan Ms(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}
