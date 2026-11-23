using System.Collections.Immutable;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.Tests.Profiles;

public sealed class MacroActionTests
{
    private static readonly KeyCombination Copy = new([0x11, 0x43]);
    private static readonly KeyCombination Paste = new([0x11, 0x56]);

    [Fact]
    public void Equals_SameKeyCombinationsAndDelay_IsTrue()
    {
        var first = new MacroAction([Copy, Paste], MacroAction.DefaultDelay);
        var second = new MacroAction([new KeyCombination([0x11, 0x43]), new KeyCombination([0x11, 0x56])], MacroAction.DefaultDelay);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentOrder_IsFalse()
    {
        Assert.NotEqual(
            new MacroAction([Copy, Paste], MacroAction.DefaultDelay),
            new MacroAction([Paste, Copy], MacroAction.DefaultDelay));
    }

    [Fact]
    public void Equals_DifferentDelay_IsFalse()
    {
        Assert.NotEqual(
            new MacroAction([Copy], MacroAction.DefaultDelay),
            new MacroAction([Copy], MacroAction.MaxDelay));
    }

    [Fact]
    public void Constructor_DefaultArray_HasNoKeyCombinations()
    {
        var macro = new MacroAction(default(ImmutableArray<KeyCombination>), MacroAction.DefaultDelay);

        Assert.Empty(macro.KeyCombinations);
    }
}
