using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.Hosting;

/// <summary>
/// What the UI shows about Desktop Mode, captured on the poll thread and handed to the UI thread. It shares nothing
/// with the poll thread's live settings.
/// </summary>
/// <param name="DesktopModeActive">Whether Desktop Mode is on.</param>
/// <param name="Profiles">Copies of the Profiles holding only their id and name, in list order.</param>
/// <param name="ActiveProfileId">The Active Profile.</param>
/// <param name="SettingsGeneration">
/// Which applied settings the snapshot reflects (see <see cref="DesktopModeHost.LatestSettingsGeneration"/>), so the
/// UI can tell a snapshot taken before its latest settings were applied.
/// </param>
internal sealed record DesktopModeSnapshot(
    bool DesktopModeActive,
    IReadOnlyList<Profile> Profiles,
    Guid ActiveProfileId,
    int SettingsGeneration)
{
    public static DesktopModeSnapshot Capture(bool desktopModeActive, AppSettings settings, int settingsGeneration) =>
        new(
            desktopModeActive,
            settings.Profiles.Select(p => new Profile { Id = p.Id, Name = p.Name }).ToList(),
            settings.ActiveProfile.Id,
            settingsGeneration);
}
