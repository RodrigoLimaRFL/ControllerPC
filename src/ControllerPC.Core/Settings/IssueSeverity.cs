namespace ControllerPC.Core.Settings;

public enum IssueSeverity
{
    /// <summary>Settings cannot be saved.</summary>
    Error,

    /// <summary>Settings can be saved, but the user should know.</summary>
    Warning,
}
