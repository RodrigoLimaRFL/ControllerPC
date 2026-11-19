namespace ControllerPC.Core.Settings;

/// <summary>The outcome of <see cref="SettingsStore.Load"/>.</summary>
/// <param name="Settings">The loaded settings, or defaults.</param>
/// <param name="CorruptFileBackupPath">
/// Set when the settings file was unreadable or invalid: it was moved to this path and defaults were returned,
/// so the app can tell the user.
/// </param>
public sealed record SettingsLoadResult(AppSettings Settings, string? CorruptFileBackupPath);
