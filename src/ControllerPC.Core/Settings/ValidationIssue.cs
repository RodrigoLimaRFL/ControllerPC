namespace ControllerPC.Core.Settings;

/// <summary>A problem found by <see cref="SettingsValidator"/>.</summary>
/// <param name="Severity">Whether it blocks saving.</param>
/// <param name="Message">User-facing explanation, using CONTEXT.md vocabulary.</param>
/// <param name="ProfileId">The Profile the issue concerns, or <c>null</c> for Base Triggers and global issues.</param>
public sealed record ValidationIssue(IssueSeverity Severity, string Message, Guid? ProfileId = null);
