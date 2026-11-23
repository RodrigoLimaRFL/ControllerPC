using ControllerPC.App.UI;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.App.Tests.UI;

public class DisplayNamesMacroTests
{
    private static readonly KeyCombination Copy = Keys(System.Windows.Forms.Keys.ControlKey, System.Windows.Forms.Keys.C);
    private static readonly KeyCombination SwitchWindow = Keys(System.Windows.Forms.Keys.Menu, System.Windows.Forms.Keys.Tab);
    private static readonly KeyCombination Paste = Keys(System.Windows.Forms.Keys.ControlKey, System.Windows.Forms.Keys.V);
    private static readonly KeyCombination Enter = Keys(System.Windows.Forms.Keys.Return);

    [Fact]
    public void ForAction_ListsEveryKeyCombinationOfAShortMacro()
    {
        var macro = new MacroAction([Copy, SwitchWindow, Paste], MacroAction.DefaultDelay);

        Assert.Equal("Macro: Ctrl+C, Alt+Tab, Ctrl+V", DisplayNames.ForAction(macro, []));
    }

    [Fact]
    public void ForAction_CutsALongMacroAfterThreeKeyCombinations()
    {
        var macro = new MacroAction([Copy, SwitchWindow, Paste, Enter], MacroAction.DefaultDelay);

        Assert.Equal("Macro: Ctrl+C, Alt+Tab, Ctrl+V, …", DisplayNames.ForAction(macro, []));
    }

    [Fact]
    public void ForAction_SaysWhenAMacroHasNoKeyCombinations()
    {
        var macro = new MacroAction([], MacroAction.DefaultDelay);

        Assert.Equal("Macro: (no Key Combinations)", DisplayNames.ForAction(macro, []));
    }

    [Fact]
    public void ForMacroKeyCombination_NumbersFromOne()
    {
        Assert.Equal("1. Ctrl+C", DisplayNames.ForMacroKeyCombination(0, Copy));
    }

    private static KeyCombination Keys(params System.Windows.Forms.Keys[] keys) => new(keys.Select(key => (ushort)key));
}
