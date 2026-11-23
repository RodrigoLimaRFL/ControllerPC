using System.Collections.Immutable;
using ControllerPC.Core.Output;

namespace ControllerPC.Core.Profiles;

/// <summary>TEMP stub of the agreed contract until feature/macros-core is merged.</summary>
public sealed record MacroAction : ButtonAction
{
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(50);

    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(2);

    public MacroAction(IEnumerable<KeyCombination> keyCombinations, TimeSpan delay)
    {
        KeyCombinations = [.. keyCombinations];
        Delay = delay;
    }

    public ImmutableArray<KeyCombination> KeyCombinations { get; }

    public TimeSpan Delay { get; }
}
