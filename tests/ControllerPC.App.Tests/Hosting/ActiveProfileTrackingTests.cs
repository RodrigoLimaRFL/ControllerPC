using ControllerPC.App.Hosting;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.Tests.Hosting;

public class ActiveProfileTrackingTests
{
    private const int LatestGeneration = 2;

    private readonly Profile _first = new() { Name = "First" };
    private readonly Profile _second = new() { Name = "Second" };
    private readonly AppSettings _saved;

    public ActiveProfileTrackingTests()
    {
        _saved = new AppSettings { Profiles = [_first, _second], ActiveProfileId = _first.Id };
    }

    [Fact]
    public void ShouldPersist_IsTrueForANewActiveProfileFromTheLatestSettings()
    {
        var snapshot = Snapshot(_second.Id, LatestGeneration);

        Assert.True(ActiveProfileTracking.ShouldPersist(snapshot, LatestGeneration, _saved));
    }

    [Fact]
    public void ShouldPersist_IsFalseWhenTheActiveProfileIsUnchanged()
    {
        var snapshot = Snapshot(_first.Id, LatestGeneration);

        Assert.False(ActiveProfileTracking.ShouldPersist(snapshot, LatestGeneration, _saved));
    }

    [Fact]
    public void ShouldPersist_IsFalseForASnapshotOfOlderSettings()
    {
        var snapshot = Snapshot(_second.Id, LatestGeneration - 1);

        Assert.False(ActiveProfileTracking.ShouldPersist(snapshot, LatestGeneration, _saved));
    }

    [Fact]
    public void ShouldPersist_IsFalseForAProfileMissingFromTheSavedSettings()
    {
        var snapshot = Snapshot(Guid.NewGuid(), LatestGeneration);

        Assert.False(ActiveProfileTracking.ShouldPersist(snapshot, LatestGeneration, _saved));
    }

    [Fact]
    public void CarryOver_SetsTheActiveProfileWhenItExists()
    {
        ActiveProfileTracking.CarryOver(_saved, _second.Id);

        Assert.Equal(_second.Id, _saved.ActiveProfileId);
    }

    [Fact]
    public void CarryOver_KeepsTheEditedActiveProfileWhenTheGivenOneWasDeleted()
    {
        ActiveProfileTracking.CarryOver(_saved, Guid.NewGuid());

        Assert.Equal(_first.Id, _saved.ActiveProfileId);
    }

    private DesktopModeSnapshot Snapshot(Guid activeProfileId, int generation) =>
        new(DesktopModeActive: false, [_first, _second], activeProfileId, generation);
}
