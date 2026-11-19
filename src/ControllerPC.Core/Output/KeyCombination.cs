using System.Collections.Immutable;

namespace ControllerPC.Core.Output;

/// <summary>
/// A Key Combination: one or more keyboard keys pressed together, as Windows virtual-key codes
/// in press order (modifiers first, e.g. Ctrl, Alt, Tab).
/// </summary>
public sealed record KeyCombination
{
    public KeyCombination(IEnumerable<ushort> virtualKeys)
    {
        VirtualKeys = [.. virtualKeys];
    }

    public ImmutableArray<ushort> VirtualKeys { get; }

    public bool IsEmpty => VirtualKeys.IsEmpty;

    public bool Equals(KeyCombination? other) =>
        other is not null && VirtualKeys.SequenceEqual(other.VirtualKeys);

    public override int GetHashCode() =>
        VirtualKeys.Aggregate(17, (hash, key) => (hash * 31) + key);

    public override string ToString() =>
        string.Join("+", VirtualKeys.Select(key => $"0x{key:X2}"));
}
