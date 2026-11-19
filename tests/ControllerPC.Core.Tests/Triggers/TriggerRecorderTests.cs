using ControllerPC.Core.Triggers;
using static ControllerPC.Core.Controller.ControllerButton;

namespace ControllerPC.Core.Tests.Triggers;

public class TriggerRecorderTests
{
    private static readonly TimeSpan Idle = TimeSpan.FromMilliseconds(1500);

    // Grouping

    [Fact]
    public void Steps_IsEmptyRightAfterStart()
    {
        var recorder = StartedRecorder();

        Assert.Empty(recorder.Steps);
    }

    [Fact]
    public void Update_GroupBecomesStepWhenEverythingIsReleased()
    {
        var recorder = StartedRecorder();

        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));

        Assert.Equal([new Chord(A)], recorder.Steps);
    }

    [Fact]
    public void Steps_ExcludesGroupStillHeld()
    {
        var recorder = StartedRecorder();

        recorder.Update(A, Ms(0));

        Assert.Empty(recorder.Steps);
    }

    [Fact]
    public void Update_StepIsUnionOfButtonsHeldDuringGroup()
    {
        var recorder = StartedRecorder();

        recorder.Update(A, Ms(0));
        recorder.Update(A | B, Ms(50));
        recorder.Update(B, Ms(100));
        recorder.Update(B | X, Ms(150));
        recorder.Update(None, Ms(200));

        Assert.Equal([new Chord(A | B | X)], recorder.Steps);
    }

    [Fact]
    public void Update_EachFullReleaseSeparatesSteps()
    {
        var recorder = StartedRecorder();

        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));
        recorder.Update(A, Ms(200));
        recorder.Update(None, Ms(300));

        Assert.Equal([new Chord(A), new Chord(A)], recorder.Steps);
    }

    [Fact]
    public void Update_RecordsMixedSequenceInOrder()
    {
        var recorder = StartedRecorder();

        recorder.Update(LeftShoulder | RightShoulder, Ms(0));
        recorder.Update(None, Ms(100));
        recorder.Update(A, Ms(200));
        recorder.Update(None, Ms(300));

        Assert.Equal([new Chord(LeftShoulder | RightShoulder), new Chord(A)], recorder.Steps);
    }

    [Fact]
    public void Update_RepeatedNothingHeldAddsNoSteps()
    {
        var recorder = StartedRecorder();

        recorder.Update(None, Ms(0));
        recorder.Update(None, Ms(100));

        Assert.Empty(recorder.Steps);
    }

    // Start

    [Fact]
    public void Start_IgnoresButtonsHeldAtStartWhileTheyStayHeld()
    {
        var recorder = new TriggerRecorder();
        recorder.Start(A, Ms(0));

        recorder.Update(A, Ms(100));
        recorder.Update(A | B, Ms(200));
        recorder.Update(A, Ms(300));

        Assert.Equal([new Chord(B)], recorder.Steps);
    }

    [Fact]
    public void Start_ButtonsHeldAtStartContributeAfterRelease()
    {
        var recorder = new TriggerRecorder();
        recorder.Start(A, Ms(0));

        recorder.Update(None, Ms(100));
        recorder.Update(A, Ms(200));
        recorder.Update(None, Ms(300));

        Assert.Equal([new Chord(A)], recorder.Steps);
    }

    [Fact]
    public void Start_DiscardsPreviousRecording()
    {
        var recorder = StartedRecorder();
        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));

        recorder.Start(None, Ms(200));

        Assert.Empty(recorder.Steps);
    }

    [Fact]
    public void Start_DiscardsGroupInProgress()
    {
        var recorder = StartedRecorder();
        recorder.Update(A, Ms(0));

        recorder.Start(None, Ms(100));
        recorder.Update(None, Ms(200));

        Assert.Empty(recorder.Steps);
    }

    // Idle

    [Fact]
    public void IsIdleFor_FalseWhenNoStepRecorded()
    {
        var recorder = StartedRecorder();

        Assert.False(recorder.IsIdleFor(Idle, Ms(60_000)));
    }

    [Fact]
    public void IsIdleFor_TrueOnceNothingHeldForIdleDuration()
    {
        var recorder = StartedRecorder();
        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));

        Assert.True(recorder.IsIdleFor(Idle, Ms(100) + Idle));
    }

    [Fact]
    public void IsIdleFor_FalseBeforeIdleDurationElapsed()
    {
        var recorder = StartedRecorder();
        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));

        Assert.False(recorder.IsIdleFor(Idle, Ms(99) + Idle));
    }

    [Fact]
    public void IsIdleFor_FalseWhileAButtonIsHeld()
    {
        var recorder = StartedRecorder();
        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));
        recorder.Update(B, Ms(200));

        Assert.False(recorder.IsIdleFor(Idle, Ms(10_000)));
    }

    [Fact]
    public void IsIdleFor_IsMeasuredFromLastRelease()
    {
        var recorder = StartedRecorder();
        recorder.Update(A, Ms(0));
        recorder.Update(None, Ms(100));
        recorder.Update(B, Ms(1000));
        recorder.Update(None, Ms(1100));

        Assert.False(recorder.IsIdleFor(Idle, Ms(100) + Idle));
    }

    private static TriggerRecorder StartedRecorder()
    {
        var recorder = new TriggerRecorder();
        recorder.Start(None, TimeSpan.Zero);
        return recorder;
    }

    private static TimeSpan Ms(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}
