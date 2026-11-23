using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>The one place that turns domain values into the text shown in the UI.</summary>
internal static class DisplayNames
{
    private const string ChordSeparator = " + ";
    private const string KeySeparator = "+";
    private const string ListSeparator = ", ";
    private const int MacroSummaryLength = 3;

    private static readonly Dictionary<ControllerButton, string> ButtonNames = new()
    {
        [ControllerButton.A] = "A",
        [ControllerButton.B] = "B",
        [ControllerButton.X] = "X",
        [ControllerButton.Y] = "Y",
        [ControllerButton.LeftShoulder] = "LB",
        [ControllerButton.RightShoulder] = "RB",
        [ControllerButton.LeftTrigger] = "LT",
        [ControllerButton.RightTrigger] = "RT",
        [ControllerButton.Start] = "Start",
        [ControllerButton.Back] = "Back",
        [ControllerButton.LeftThumb] = "LS click",
        [ControllerButton.RightThumb] = "RS click",
        [ControllerButton.DPadUp] = "D-pad Up",
        [ControllerButton.DPadDown] = "D-pad Down",
        [ControllerButton.DPadLeft] = "D-pad Left",
        [ControllerButton.DPadRight] = "D-pad Right",
    };

    // Keys has several names per value (Return/Enter, Next/PageDown...), so Enum.ToString would be ambiguous.
    private static readonly Dictionary<Keys, string> KeyNames = new()
    {
        [Keys.ControlKey] = "Ctrl",
        [Keys.LControlKey] = "Ctrl",
        [Keys.RControlKey] = "Ctrl",
        [Keys.Menu] = "Alt",
        [Keys.LMenu] = "Alt",
        [Keys.RMenu] = "Alt",
        [Keys.ShiftKey] = "Shift",
        [Keys.LShiftKey] = "Shift",
        [Keys.RShiftKey] = "Shift",
        [Keys.LWin] = "Win",
        [Keys.RWin] = "Win",
        [Keys.Return] = "Enter",
        [Keys.Escape] = "Esc",
        [Keys.Back] = "Backspace",
        [Keys.Capital] = "Caps Lock",
        [Keys.Prior] = "Page Up",
        [Keys.Next] = "Page Down",
        [Keys.Snapshot] = "Print Screen",
        [Keys.Apps] = "Menu",
        [Keys.D0] = "0",
        [Keys.D1] = "1",
        [Keys.D2] = "2",
        [Keys.D3] = "3",
        [Keys.D4] = "4",
        [Keys.D5] = "5",
        [Keys.D6] = "6",
        [Keys.D7] = "7",
        [Keys.D8] = "8",
        [Keys.D9] = "9",
        [Keys.OemMinus] = "-",
        [Keys.Oemplus] = "=",
        [Keys.Oemcomma] = ",",
        [Keys.OemPeriod] = ".",
        [Keys.OemQuestion] = "/",
        [Keys.OemSemicolon] = ";",
        [Keys.OemQuotes] = "'",
        [Keys.OemOpenBrackets] = "[",
        [Keys.OemCloseBrackets] = "]",
        [Keys.OemPipe] = "\\",
        [Keys.Oemtilde] = "`",
    };

    private static readonly Dictionary<MouseButton, string> MouseButtonNames = new()
    {
        [MouseButton.Left] = "Left",
        [MouseButton.Right] = "Right",
        [MouseButton.Middle] = "Middle",
        [MouseButton.Back] = "Back",
        [MouseButton.Forward] = "Forward",
    };

    /// <summary>Short name of one controller button, e.g. "LB" or "D-pad Up".</summary>
    public static string ForButton(ControllerButton button) =>
        ButtonNames.TryGetValue(button, out var name) ? name : button.ToString();

    /// <summary>A Chord as "LB + RB + Start", in <see cref="ControllerButtons.All"/> order.</summary>
    public static string ForChord(Chord chord) =>
        chord.IsEmpty
            ? "(no buttons)"
            : string.Join(ChordSeparator, ControllerButtons.Split(chord.Buttons).Select(ForButton));

    /// <summary>One Step in a list of Steps, numbered from 1: "1. LB + RB + Start".</summary>
    public static string ForStep(int index, Chord chord) => $"{index + 1}. {ForChord(chord)}";

    /// <summary>A whole Mode Trigger's Steps on one line, e.g. "LB + RB, then Start".</summary>
    public static string ForSteps(IEnumerable<Chord> steps)
    {
        var names = steps.Select(ForChord).ToList();
        return names.Count == 0 ? "(no Steps)" : string.Join(", then ", names);
    }

    /// <summary>A Key Combination as "Ctrl+Alt+Tab", in press order.</summary>
    public static string ForKeyCombination(KeyCombination keys) =>
        keys.IsEmpty ? "(no keys)" : string.Join(KeySeparator, keys.VirtualKeys.Select(key => ForKey((Keys)key)));

    /// <summary>One keyboard key, e.g. "Ctrl", "Enter" or "F5".</summary>
    public static string ForKey(Keys key) =>
        KeyNames.TryGetValue(key, out var name) ? name : key.ToString();

    public static string ForMouseButton(MouseButton button) => MouseButtonNames[button];

    public static string ForNudgeDirection(NudgeDirection direction) => direction.ToString();

    public static string ForRightStickRole(RightStickRole role) => role.ToString();

    /// <summary>One Key Combination of a Macro, numbered from 1: "1. Ctrl+C".</summary>
    public static string ForMacroKeyCombination(int index, KeyCombination keys) => $"{index + 1}. {ForKeyCombination(keys)}";

    /// <summary>A Chord Binding as "LB + A → Key Combination: Ctrl+C".</summary>
    public static string ForChordBinding(ChordBinding binding, IEnumerable<Profile> profiles) =>
        $"{ForChord(binding.Chord)} → {ForAction(binding.Action, profiles)}";

    /// <summary>A Macro's first few Key Combinations, e.g. "Ctrl+C, Alt+Tab, Ctrl+V, …", so list rows stay short.</summary>
    private static string ForMacroSummary(MacroAction macro)
    {
        if (macro.KeyCombinations.IsEmpty)
        {
            return "(no Key Combinations)";
        }

        var shown = macro.KeyCombinations.Take(MacroSummaryLength).Select(ForKeyCombination);
        var more = macro.KeyCombinations.Length > MacroSummaryLength ? ListSeparator + "\u2026" : string.Empty;
        return string.Join(ListSeparator, shown) + more;
    }

    public static string ForActionKind(ActionKind kind) => kind switch
    {
        ActionKind.None => "None",
        ActionKind.MouseButton => "Mouse button",
        ActionKind.KeyCombination => "Key Combination",
        ActionKind.Nudge => "Nudge",
        ActionKind.NextProfile => "Next Profile",
        ActionKind.SwitchToProfile => "Switch to Profile",
        ActionKind.Macro => "Macro",
        _ => kind.ToString(),
    };

    /// <summary>
    /// An Action as shown in the Bindings list. <paramref name="profiles"/> resolves "Switch to Profile" targets.
    /// </summary>
    public static string ForAction(ButtonAction action, IEnumerable<Profile> profiles) => action switch
    {
        NoAction => "None",
        MouseButtonAction mouse => $"Mouse button: {ForMouseButton(mouse.Button)}",
        KeyCombinationAction keys => $"Key Combination: {ForKeyCombination(keys.Keys)}",
        NudgeAction nudge => $"Nudge {ForNudgeDirection(nudge.Direction)}",
        NextProfileAction => "Next Profile",
        SwitchToProfileAction switchTo =>
            $"Switch to Profile: {profiles.FirstOrDefault(p => p.Id == switchTo.ProfileId)?.Name ?? "(missing Profile)"}",
        MacroAction macro => $"Macro: {ForMacroSummary(macro)}",
        _ => action.GetType().Name,
    };
}
