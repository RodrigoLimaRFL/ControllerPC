using ControllerPC.Core.Settings;

namespace ControllerPC.App.Hosting;

/// <summary>
/// Keeps the saved Active Profile in step with the one Desktop Mode uses: the controller and the tray switch it on
/// the poll thread, while the settings window edits a copy taken when it opened.
/// </summary>
internal static class ActiveProfileTracking
{
    /// <summary>
    /// Whether <paramref name="snapshot"/> reports an Active Profile that should be written to the
    /// <paramref name="saved"/> settings: it differs, it exists there, and the snapshot reflects the latest applied
    /// settings (an older one may name a Profile chosen before the user's latest changes).
    /// </summary>
    public static bool ShouldPersist(DesktopModeSnapshot snapshot, int latestSettingsGeneration, AppSettings saved) =>
        snapshot.SettingsGeneration == latestSettingsGeneration
        && snapshot.ActiveProfileId != saved.ActiveProfileId
        && saved.FindProfile(snapshot.ActiveProfileId) is not null;

    /// <summary>
    /// Makes <paramref name="activeProfileId"/> the Active Profile of settings edited in the settings window, when that
    /// Profile still exists there: the copy the window edited may predate a switch made in the meantime.
    /// </summary>
    public static void CarryOver(AppSettings edited, Guid activeProfileId)
    {
        if (edited.FindProfile(activeProfileId) is not null)
        {
            edited.ActiveProfileId = activeProfileId;
        }
    }
}
