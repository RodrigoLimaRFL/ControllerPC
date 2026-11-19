namespace ControllerPC.Core.Settings;

public enum IssueSeverity
{
    /// <summary>Settings cannot be saved.</summary>
    Error,

    /// <summary>Settings can be saved, but the user should know.</summary>
    Warning,
}

public sealed record ValidationIssue(IssueSeverity Severity, string Message);

/// <summary>
/// Checks settings before saving. Rules (see CONTEXT.md):
/// <list type="bullet">
/// <item>Error: no Profiles, duplicate or blank Profile names, an empty Mode Trigger, or an empty Chord.</item>
/// <item>Error: two distinct Entry Trigger sources with identical Steps (a Profile's override equal to the
/// Base Entry Trigger or to another Profile's override).</item>
/// <item>Warning: one Entry Trigger's Steps are a strict prefix of another's (the longer one can never fire).</item>
/// <item>Warning: a Profile's Effective Exit Trigger uses buttons that the same Profile binds to Key Combinations.</item>
/// </list>
/// Identical Entry and Exit Triggers are allowed.
/// </summary>
public static class SettingsValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(AppSettings settings) => throw new NotImplementedException();
}
