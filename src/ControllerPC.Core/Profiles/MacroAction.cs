using System.Collections.Immutable;
using System.Text.Json.Serialization;
using ControllerPC.Core.Output;

namespace ControllerPC.Core.Profiles;

/// <summary>
/// A Macro: taps its Key Combinations one after another, with the Macro <see cref="Delay"/> between two of them,
/// each time its button or Chord is pressed.
/// </summary>
public sealed record MacroAction : ButtonAction
{
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>The longest Macro Delay allowed.</summary>
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(2);

    public MacroAction(IEnumerable<KeyCombination> keyCombinations, TimeSpan delay)
        : this([.. keyCombinations], delay)
    {
    }

    [JsonConstructor]
    public MacroAction(ImmutableArray<KeyCombination> keyCombinations, TimeSpan delay)
    {
        KeyCombinations = keyCombinations.IsDefault ? [] : keyCombinations;
        Delay = delay;
    }

    /// <summary>The Key Combinations, in the order they are tapped.</summary>
    public ImmutableArray<KeyCombination> KeyCombinations { get; }

    /// <summary>The Macro Delay: the pause between releasing one Key Combination and pressing the next.</summary>
    public TimeSpan Delay { get; }

    public bool Equals(MacroAction? other) =>
        other is not null && Delay == other.Delay && KeyCombinations.SequenceEqual(other.KeyCombinations);

    public override int GetHashCode() =>
        KeyCombinations.Aggregate(Delay.GetHashCode(), (hash, keys) => (hash * 31) + keys.GetHashCode());
}
