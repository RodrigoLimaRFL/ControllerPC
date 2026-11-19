using ControllerPC.Windows.Diagnostics;

namespace ControllerPC.Windows.Tests.Diagnostics;

public class StatusChangeFilterTests
{
    private const uint Success = 0;
    private const uint Failure = 5;
    private const uint OtherFailure = 6;

    [Fact]
    public void Changed_IsFalseForTheInitialStatus()
    {
        var filter = new StatusChangeFilter(sourceCount: 1, Success);

        Assert.False(filter.Changed(0, Success));
    }

    [Fact]
    public void Changed_IsTrueForAFailureAfterSuccess()
    {
        var filter = new StatusChangeFilter(sourceCount: 1, Success);

        Assert.True(filter.Changed(0, Failure));
    }

    [Fact]
    public void Changed_IsFalseForARepeatedFailure()
    {
        var filter = new StatusChangeFilter(sourceCount: 1, Success);
        filter.Changed(0, Failure);

        Assert.False(filter.Changed(0, Failure));
    }

    [Fact]
    public void Changed_IsTrueAgainForAFailureAfterRecovery()
    {
        var filter = new StatusChangeFilter(sourceCount: 1, Success);
        filter.Changed(0, Failure);
        filter.Changed(0, Success);

        Assert.True(filter.Changed(0, Failure));
    }

    [Fact]
    public void Changed_IsTrueWhenTheFailureKindChanges()
    {
        var filter = new StatusChangeFilter(sourceCount: 1, Success);
        filter.Changed(0, Failure);

        Assert.True(filter.Changed(0, OtherFailure));
    }

    [Fact]
    public void Changed_TracksEachSourceSeparately()
    {
        var filter = new StatusChangeFilter(sourceCount: 2, Success);
        filter.Changed(0, Failure);

        Assert.True(filter.Changed(1, Failure));
    }
}
