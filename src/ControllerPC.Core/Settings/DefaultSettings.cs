namespace ControllerPC.Core.Settings;

/// <summary>
/// Builds the settings of a fresh install:
/// <list type="bullet">
/// <item>Base Entry Trigger and Base Exit Trigger: one Step <c>[LB+RB+Start]</c> with Final Hold enabled (default duration).</item>
/// <item>One "Default" Profile (the Active Profile), right stick scrolls, default tuning, Bindings:
/// LB left click, RB right click, D-pad Nudges, A Enter, B Esc, Y on-screen keyboard (<c>Win+Ctrl+O</c>);
/// all other buttons unbound.</item>
/// </list>
/// The result passes <see cref="SettingsValidator"/> with no issues.
/// </summary>
public static class DefaultSettings
{
    public static AppSettings Create() => throw new NotImplementedException();
}
