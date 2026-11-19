using ControllerPC.Core.Controller;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Watches every controller slot for the Entry Triggers watched outside Desktop Mode: one
/// <see cref="TriggerMatcher"/> per slot and <see cref="WatchedEntryTrigger"/>.
/// </summary>
/// <remarks>
/// Matchers capture their trigger when created, so build a new watcher whenever the watched Entry Triggers change.
/// </remarks>
internal sealed class EntryWatcher
{
    private readonly IReadOnlyList<WatchedEntryTrigger> _watched;

    // _matchersBySlot[slot][i] watches _watched[i], so a fired matcher maps back by index, not by trigger contents.
    private readonly TriggerMatcher[][] _matchersBySlot;

    public EntryWatcher(IReadOnlyList<WatchedEntryTrigger> watched, int slotCount)
    {
        _watched = watched;
        _matchersBySlot = Enumerable.Range(0, slotCount)
            .Select(_ => watched.Select(entry => new TriggerMatcher(entry.Trigger)).ToArray())
            .ToArray();
    }

    /// <summary>
    /// Feeds <paramref name="held"/> from <paramref name="slot"/> to each of its matchers. Returns the first watched
    /// Entry Trigger (in watched order) that fired, if any.
    /// </summary>
    public WatchedEntryTrigger? Update(int slot, ControllerButton held, TimeSpan now)
    {
        WatchedEntryTrigger? fired = null;
        var matchers = _matchersBySlot[slot];
        for (var index = 0; index < matchers.Length; index++)
        {
            if (matchers[index].Update(held, now))
            {
                fired ??= _watched[index];
            }
        }

        return fired;
    }

    /// <summary>Abandons the attempts on <paramref name="slot"/>; <paramref name="held"/> must be released before contributing.</summary>
    public void Reset(int slot, ControllerButton held)
    {
        foreach (var matcher in _matchersBySlot[slot])
        {
            matcher.Reset(held);
        }
    }
}
