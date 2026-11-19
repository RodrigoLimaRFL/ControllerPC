using ControllerPC.Core.Output;

namespace ControllerPC.Core.Tests.Fakes;

/// <summary>An <see cref="IInputSink"/> that records every call, for asserting what a translator sent.</summary>
public sealed class RecordingInputSink : IInputSink
{
    private readonly Dictionary<ushort, int> _keyDownCounts = [];
    private readonly Dictionary<MouseButton, int> _mouseButtonDownCounts = [];

    public List<(int Dx, int Dy)> CursorMoves { get; } = [];

    public List<(int Vertical, int Horizontal)> Scrolls { get; } = [];

    /// <summary>Key and mouse button presses and releases in call order, e.g. <c>"key down 0x11"</c>, <c>"mouse up Left"</c>.</summary>
    public List<string> ButtonEvents { get; } = [];

    /// <summary>Each <see cref="PressKeys"/> and <see cref="ReleaseKeys"/> call, e.g. <c>"press 0x11+0x43"</c>.</summary>
    public List<string> KeyCalls { get; } = [];

    public int TotalDx => CursorMoves.Sum(move => move.Dx);

    public int TotalDy => CursorMoves.Sum(move => move.Dy);

    public int TotalVerticalScroll => Scrolls.Sum(scroll => scroll.Vertical);

    public int TotalHorizontalScroll => Scrolls.Sum(scroll => scroll.Horizontal);

    /// <summary>Keys pressed more times than released.</summary>
    public IEnumerable<ushort> KeysDown => _keyDownCounts.Where(entry => entry.Value > 0).Select(entry => entry.Key);

    /// <summary>Mouse buttons pressed more times than released.</summary>
    public IEnumerable<MouseButton> MouseButtonsDown =>
        _mouseButtonDownCounts.Where(entry => entry.Value > 0).Select(entry => entry.Key);

    public void MoveCursor(int dx, int dy) => CursorMoves.Add((dx, dy));

    public void Scroll(int vertical, int horizontal) => Scrolls.Add((vertical, horizontal));

    public void PressMouseButton(MouseButton button)
    {
        ButtonEvents.Add(MouseEvent("down", button));
        _mouseButtonDownCounts[button] = _mouseButtonDownCounts.GetValueOrDefault(button) + 1;
    }

    public void ReleaseMouseButton(MouseButton button)
    {
        ButtonEvents.Add(MouseEvent("up", button));
        _mouseButtonDownCounts[button] = _mouseButtonDownCounts.GetValueOrDefault(button) - 1;
    }

    public void PressKeys(KeyCombination keys)
    {
        KeyCalls.Add(PressCall(keys.VirtualKeys.ToArray()));
        foreach (var key in keys.VirtualKeys)
        {
            ButtonEvents.Add(KeyEvent("down", key));
            _keyDownCounts[key] = _keyDownCounts.GetValueOrDefault(key) + 1;
        }
    }

    public void ReleaseKeys(KeyCombination keys)
    {
        KeyCalls.Add(ReleaseCall(keys.VirtualKeys.ToArray()));
        foreach (var key in keys.VirtualKeys.Reverse())
        {
            ButtonEvents.Add(KeyEvent("up", key));
            _keyDownCounts[key] = _keyDownCounts.GetValueOrDefault(key) - 1;
        }
    }

    public static string KeyEvent(string direction, ushort key) => $"key {direction} 0x{key:X2}";

    /// <param name="keys">The combination passed to <see cref="PressKeys"/>, in its order.</param>
    public static string PressCall(params ushort[] keys) => $"press {new KeyCombination(keys)}";

    /// <param name="keys">The combination passed to <see cref="ReleaseKeys"/>, in its (press) order.</param>
    public static string ReleaseCall(params ushort[] keys) => $"release {new KeyCombination(keys)}";

    public static string MouseEvent(string direction, MouseButton button) => $"mouse {direction} {button}";
}
