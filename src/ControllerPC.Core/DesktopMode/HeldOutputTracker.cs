using ControllerPC.Core.Output;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Reference-counts the keys and mouse buttons held by Bindings, so one shared by two held Bindings (e.g. Ctrl)
/// goes down with the first and comes up only after the last is let go.
/// </summary>
/// <remarks>
/// Keys are counted per virtual key. Pressing a Key Combination sends the sink one <see cref="IInputSink.PressKeys"/>
/// call with just the keys that were not held yet, in combination order; releasing sends one
/// <see cref="IInputSink.ReleaseKeys"/> call with just the keys no other Binding still holds, which the sink
/// releases in reverse. Each Key Combination thus stays a single, atomic sink call.
/// </remarks>
internal sealed class HeldOutputTracker
{
    private readonly IInputSink _sink;
    private readonly Dictionary<ushort, int> _keyHoldCounts = [];
    private readonly List<ushort> _heldKeysInPressOrder = [];
    private readonly Dictionary<MouseButton, int> _mouseButtonHoldCounts = [];

    public HeldOutputTracker(IInputSink sink)
    {
        _sink = sink;
    }

    public void PressKeys(KeyCombination keys)
    {
        var newlyHeld = new List<ushort>();
        foreach (var key in keys.VirtualKeys)
        {
            if (AddHold(_keyHoldCounts, key))
            {
                newlyHeld.Add(key);
            }
        }

        if (newlyHeld.Count > 0)
        {
            _heldKeysInPressOrder.AddRange(newlyHeld);
            _sink.PressKeys(new KeyCombination(newlyHeld));
        }
    }

    public void ReleaseKeys(KeyCombination keys)
    {
        var noLongerHeld = new List<ushort>();
        for (var index = keys.VirtualKeys.Length - 1; index >= 0; index--)
        {
            var key = keys.VirtualKeys[index];
            if (RemoveHold(_keyHoldCounts, key))
            {
                noLongerHeld.Add(key);
                _heldKeysInPressOrder.Remove(key);
            }
        }

        if (noLongerHeld.Count > 0)
        {
            // Collected in release order; the sink expects press order and releases in reverse.
            noLongerHeld.Reverse();
            _sink.ReleaseKeys(new KeyCombination(noLongerHeld));
        }
    }

    public void PressMouseButton(MouseButton button)
    {
        if (AddHold(_mouseButtonHoldCounts, button))
        {
            _sink.PressMouseButton(button);
        }
    }

    public void ReleaseMouseButton(MouseButton button)
    {
        if (RemoveHold(_mouseButtonHoldCounts, button))
        {
            _sink.ReleaseMouseButton(button);
        }
    }

    /// <summary>Releases every held mouse button, then every held key in reverse press order, whatever their counts.</summary>
    public void ReleaseAll()
    {
        foreach (var button in _mouseButtonHoldCounts.Keys)
        {
            _sink.ReleaseMouseButton(button);
        }

        _mouseButtonHoldCounts.Clear();

        if (_heldKeysInPressOrder.Count > 0)
        {
            _sink.ReleaseKeys(new KeyCombination(_heldKeysInPressOrder));
        }

        _heldKeysInPressOrder.Clear();
        _keyHoldCounts.Clear();
    }

    /// <returns><c>true</c> when <paramref name="item"/> was not held before.</returns>
    private static bool AddHold<T>(Dictionary<T, int> holdCounts, T item)
        where T : notnull
    {
        holdCounts.TryGetValue(item, out var count);
        holdCounts[item] = count + 1;
        return count == 0;
    }

    /// <returns><c>true</c> when the last hold of <paramref name="item"/> was removed.</returns>
    private static bool RemoveHold<T>(Dictionary<T, int> holdCounts, T item)
        where T : notnull
    {
        if (!holdCounts.TryGetValue(item, out var count))
        {
            return false;
        }

        if (count > 1)
        {
            holdCounts[item] = count - 1;
            return false;
        }

        holdCounts.Remove(item);
        return true;
    }
}
