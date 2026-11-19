using ControllerPC.Core.Output;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Reference-counts the keys and mouse buttons held by Bindings, so one shared by two held Bindings (e.g. Ctrl)
/// goes down with the first and comes up only after the last is let go.
/// </summary>
/// <remarks>
/// Keys are counted per virtual key and sent to the sink as single-key Key Combinations as each one goes from
/// unheld to held or back. That keeps a Key Combination's press order (in order) and release order (in reverse)
/// while skipping the keys another Binding still holds.
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
        foreach (var key in keys.VirtualKeys)
        {
            if (AddHold(_keyHoldCounts, key))
            {
                _heldKeysInPressOrder.Add(key);
                _sink.PressKeys(SingleKey(key));
            }
        }
    }

    public void ReleaseKeys(KeyCombination keys)
    {
        for (var index = keys.VirtualKeys.Length - 1; index >= 0; index--)
        {
            var key = keys.VirtualKeys[index];
            if (RemoveHold(_keyHoldCounts, key))
            {
                _heldKeysInPressOrder.Remove(key);
                _sink.ReleaseKeys(SingleKey(key));
            }
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

        for (var index = _heldKeysInPressOrder.Count - 1; index >= 0; index--)
        {
            _sink.ReleaseKeys(SingleKey(_heldKeysInPressOrder[index]));
        }

        _heldKeysInPressOrder.Clear();
        _keyHoldCounts.Clear();
    }

    private static KeyCombination SingleKey(ushort key) => new([key]);

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
