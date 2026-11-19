using ControllerPC.App.Hosting;

namespace ControllerPC.App.Tests.Hosting;

public class PauseCounterTests
{
    private readonly PauseCounter _pauses = new();

    [Fact]
    public void Pause_IsTrueForTheFirstRequest()
    {
        Assert.True(_pauses.Pause());
    }

    [Fact]
    public void Pause_IsFalseForAnOverlappingRequest()
    {
        _pauses.Pause();

        Assert.False(_pauses.Pause());
    }

    [Fact]
    public void Resume_IsFalseWhileAnotherRequestIsOpen()
    {
        _pauses.Pause();
        _pauses.Pause();

        Assert.False(_pauses.Resume());
    }

    [Fact]
    public void Resume_IsTrueWhenTheLastRequestEnds()
    {
        _pauses.Pause();
        _pauses.Pause();
        _pauses.Resume();

        Assert.True(_pauses.Resume());
    }

    [Fact]
    public void Resume_IsIgnoredWhenNothingIsPaused()
    {
        Assert.False(_pauses.Resume());
    }

    [Fact]
    public void Pause_AfterAnUnmatchedResumeStillPausesOnce()
    {
        _pauses.Resume();
        _pauses.Pause();

        Assert.True(_pauses.Resume());
    }
}
