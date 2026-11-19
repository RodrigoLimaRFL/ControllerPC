using ControllerPC.App.Hosting;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.Tests.Hosting;

public class DesktopModeSnapshotTests
{
    [Fact]
    public void Capture_KeepsProfileIdsAndNamesInListOrder()
    {
        var settings = TwoProfiles(out var first, out var second);

        var snapshot = DesktopModeSnapshot.Capture(desktopModeActive: false, settings, settingsGeneration: 0);

        Assert.Equal([(first.Id, "First"), (second.Id, "Second")], snapshot.Profiles.Select(p => (p.Id, p.Name)));
    }

    [Fact]
    public void Capture_DoesNotShareProfilesWithTheSettings()
    {
        var settings = TwoProfiles(out var first, out _);
        var snapshot = DesktopModeSnapshot.Capture(desktopModeActive: false, settings, settingsGeneration: 0);

        first.Name = "Renamed";

        Assert.Equal("First", snapshot.Profiles[0].Name);
    }

    [Fact]
    public void Capture_UsesTheActiveProfile()
    {
        var settings = TwoProfiles(out _, out var second);
        settings.ActiveProfileId = second.Id;

        var snapshot = DesktopModeSnapshot.Capture(desktopModeActive: false, settings, settingsGeneration: 0);

        Assert.Equal(second.Id, snapshot.ActiveProfileId);
    }

    [Fact]
    public void Capture_FallsBackToTheFirstProfileForAnUnknownActiveId()
    {
        var settings = TwoProfiles(out var first, out _);
        settings.ActiveProfileId = Guid.NewGuid();

        var snapshot = DesktopModeSnapshot.Capture(desktopModeActive: false, settings, settingsGeneration: 0);

        Assert.Equal(first.Id, snapshot.ActiveProfileId);
    }

    [Fact]
    public void Capture_KeepsDesktopModeStateAndGeneration()
    {
        var settings = TwoProfiles(out _, out _);

        var snapshot = DesktopModeSnapshot.Capture(desktopModeActive: true, settings, settingsGeneration: 3);

        Assert.Equal((true, 3), (snapshot.DesktopModeActive, snapshot.SettingsGeneration));
    }

    private static AppSettings TwoProfiles(out Profile first, out Profile second)
    {
        first = new Profile { Name = "First" };
        second = new Profile { Name = "Second" };
        return new AppSettings { Profiles = [first, second], ActiveProfileId = first.Id };
    }
}
