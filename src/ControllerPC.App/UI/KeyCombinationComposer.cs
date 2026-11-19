using ControllerPC.Core.Output;

namespace ControllerPC.App.UI;

/// <summary>
/// Builds Key Combinations from captured keys in press order: modifiers first (Win, Ctrl, Alt, Shift), then the
/// other key. WinForms <see cref="Keys"/> key codes are Windows virtual-key codes.
/// </summary>
internal static class KeyCombinationComposer
{
    /// <summary>The modifier keys, in the order they are pressed.</summary>
    public static IReadOnlyList<Keys> ModifierOrder { get; } = [Keys.LWin, Keys.ControlKey, Keys.Menu, Keys.ShiftKey];

    /// <summary>
    /// The generic modifier key for a key code (left/right variants and RWin collapse onto it), or <c>null</c>
    /// when <paramref name="key"/> is not a modifier.
    /// </summary>
    public static Keys? ToModifier(Keys key) => key switch
    {
        Keys.LWin or Keys.RWin => Keys.LWin,
        Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => Keys.ControlKey,
        Keys.Menu or Keys.LMenu or Keys.RMenu => Keys.Menu,
        Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => Keys.ShiftKey,
        _ => null,
    };

    /// <summary>The Ctrl/Alt/Shift modifier keys named by the modifier flags of <paramref name="keyData"/>.</summary>
    public static IEnumerable<Keys> ModifiersOf(Keys keyData)
    {
        if (keyData.HasFlag(Keys.Control))
        {
            yield return Keys.ControlKey;
        }

        if (keyData.HasFlag(Keys.Alt))
        {
            yield return Keys.Menu;
        }

        if (keyData.HasFlag(Keys.Shift))
        {
            yield return Keys.ShiftKey;
        }
    }

    /// <summary>Combines <paramref name="modifiers"/> (any order, duplicates allowed) with an optional other key.</summary>
    public static KeyCombination Compose(IEnumerable<Keys> modifiers, Keys? otherKey)
    {
        var held = modifiers.Select(ToModifier).OfType<Keys>().ToHashSet();
        var keys = ModifierOrder.Where(held.Contains).ToList();
        if (otherKey is { } key)
        {
            keys.Add(key);
        }

        return new KeyCombination(keys.Select(k => (ushort)k));
    }

    /// <summary>Whether <paramref name="keys"/> contains a Win key.</summary>
    public static bool HasWin(KeyCombination keys) =>
        keys.VirtualKeys.Any(key => ToModifier((Keys)key) == Keys.LWin);

    /// <summary><paramref name="keys"/> with Win added in first position, or removed.</summary>
    public static KeyCombination WithWin(KeyCombination keys, bool includeWin)
    {
        var others = keys.VirtualKeys.Where(key => ToModifier((Keys)key) != Keys.LWin);
        return new KeyCombination(includeWin ? others.Prepend((ushort)Keys.LWin) : others);
    }
}
