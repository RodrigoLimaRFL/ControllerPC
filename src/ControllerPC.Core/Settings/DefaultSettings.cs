using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

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
    private const string DefaultProfileName = "Default";

    private const ushort VirtualKeyEnter = 0x0D;
    private const ushort VirtualKeyEscape = 0x1B;
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const ushort VirtualKeyO = 0x4F;

    private const ControllerButton ModeTriggerChord =
        ControllerButton.LeftShoulder | ControllerButton.RightShoulder | ControllerButton.Start;

    public static AppSettings Create()
    {
        var profile = CreateDefaultProfile();
        return new AppSettings
        {
            BaseEntryTrigger = CreateBaseTrigger(),
            BaseExitTrigger = CreateBaseTrigger(),
            Profiles = [profile],
            ActiveProfileId = profile.Id,
        };
    }

    private static ModeTrigger CreateBaseTrigger() => new()
    {
        Steps = [new Chord(ModeTriggerChord)],
        FinalHoldEnabled = true,
    };

    private static Profile CreateDefaultProfile() => new()
    {
        Name = DefaultProfileName,
        RightStickRole = RightStickRole.Scroll,
        Tuning = new MovementTuning(),
        Bindings = new Dictionary<ControllerButton, ButtonAction>
        {
            [ControllerButton.LeftShoulder] = new MouseButtonAction(MouseButton.Left),
            [ControllerButton.RightShoulder] = new MouseButtonAction(MouseButton.Right),
            [ControllerButton.DPadUp] = new NudgeAction(NudgeDirection.Up),
            [ControllerButton.DPadDown] = new NudgeAction(NudgeDirection.Down),
            [ControllerButton.DPadLeft] = new NudgeAction(NudgeDirection.Left),
            [ControllerButton.DPadRight] = new NudgeAction(NudgeDirection.Right),
            [ControllerButton.A] = Keys(VirtualKeyEnter),
            [ControllerButton.B] = Keys(VirtualKeyEscape),
            [ControllerButton.Y] = Keys(VirtualKeyLeftWindows, VirtualKeyControl, VirtualKeyO),
        },
    };

    private static KeyCombinationAction Keys(params ushort[] virtualKeys) =>
        new(new KeyCombination(virtualKeys));
}
