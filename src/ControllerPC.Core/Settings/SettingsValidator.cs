namespace ControllerPC.Core.Settings;

/// <summary>
/// Checks settings before saving.
/// <para>Errors (block saving):</para>
/// <list type="bullet">
/// <item>No Profiles; duplicate Profile ids; blank or duplicate Profile names (case-insensitive).</item>
/// <item>A Mode Trigger (Base or override) with no Steps, or with an empty Chord.</item>
/// <item>A Mode Trigger with Step Gap or Final Hold duration &lt;= 0.</item>
/// <item>Two watched Entry Trigger sources with identical Steps: a Profile's Entry Trigger Override equal to
/// the Base Entry Trigger or to another Profile's override.</item>
/// <item>A Binding whose key is <c>None</c> or more than one button, or a Key Combination with no keys.</item>
/// <item>Tuning out of range: Deadzone outside [0, 1), CursorSpeed or ScrollSpeed &lt;= 0, NudgeStep &lt; 1.</item>
/// </list>
/// <para>Warnings (saving allowed):</para>
/// <list type="bullet">
/// <item>One Entry Trigger's Steps are a strict prefix of another's (the longer one can never fire).</item>
/// <item>A Profile's Effective Exit Trigger uses buttons that the same Profile binds to Key Combinations.</item>
/// <item>A "Switch to Profile" Action pointing at a Profile that no longer exists.</item>
/// </list>
/// Identical Entry and Exit Triggers are allowed. Two Mode Triggers are identical when their Steps are equal;
/// timing settings are not compared.
/// </summary>
public static class SettingsValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(AppSettings settings) => throw new NotImplementedException();
}
