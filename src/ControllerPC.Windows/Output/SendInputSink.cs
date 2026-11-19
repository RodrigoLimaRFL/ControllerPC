using System.Diagnostics;
using System.Runtime.InteropServices;
using ControllerPC.Core.Output;

namespace ControllerPC.Windows.Output;

/// <summary>
/// Sends synthetic mouse and keyboard input through the Win32 SendInput API. Relative moves are subject to the
/// user's pointer-speed settings, so pixels are approximate. Extended keys (arrows, navigation keys, right-hand
/// modifiers, Win) are sent with <c>KEYEVENTF_EXTENDEDKEY</c>.
/// </summary>
/// <remarks>
/// Each method sends its events in a single SendInput call so they cannot be interleaved with other input.
/// Windows silently drops synthetic input aimed at a higher-integrity window (UIPI); that is logged, not thrown,
/// because it is an expected condition rather than a bug.
/// </remarks>
public sealed class SendInputSink : IInputSink
{
    private const int MaxScrollEvents = 2;

    public void MoveCursor(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        Send([MouseEvent(User32Native.MouseEventMove, dx, dy, mouseData: 0)]);
    }

    public void Scroll(int vertical, int horizontal)
    {
        Span<NativeInput> inputs = stackalloc NativeInput[MaxScrollEvents];
        int count = 0;
        if (vertical != 0)
        {
            inputs[count++] = MouseEvent(User32Native.MouseEventWheel, 0, 0, unchecked((uint)vertical));
        }

        if (horizontal != 0)
        {
            inputs[count++] = MouseEvent(User32Native.MouseEventHorizontalWheel, 0, 0, unchecked((uint)horizontal));
        }

        Send(inputs[..count]);
    }

    public void PressMouseButton(MouseButton button)
    {
        (uint down, _, uint mouseData) = MouseButtonFlags(button);
        Send([MouseEvent(down, 0, 0, mouseData)]);
    }

    public void ReleaseMouseButton(MouseButton button)
    {
        (_, uint up, uint mouseData) = MouseButtonFlags(button);
        Send([MouseEvent(up, 0, 0, mouseData)]);
    }

    public void PressKeys(KeyCombination keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var inputs = new NativeInput[keys.VirtualKeys.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            ushort virtualKey = keys.VirtualKeys[i];
            inputs[i] = KeyEvent(virtualKey, KeyDownFlags(virtualKey));
        }

        Send(inputs);
    }

    public void ReleaseKeys(KeyCombination keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var inputs = new NativeInput[keys.VirtualKeys.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            ushort virtualKey = keys.VirtualKeys[^(i + 1)];
            inputs[i] = KeyEvent(virtualKey, KeyDownFlags(virtualKey) | User32Native.KeyEventKeyUp);
        }

        Send(inputs);
    }

    /// <summary>The <c>MOUSEEVENTF_*</c> flags that press and release <paramref name="button"/>, and its <c>mouseData</c>.</summary>
    internal static (uint Down, uint Up, uint MouseData) MouseButtonFlags(MouseButton button) =>
        button switch
        {
            MouseButton.Left => (User32Native.MouseEventLeftDown, User32Native.MouseEventLeftUp, 0),
            MouseButton.Right => (User32Native.MouseEventRightDown, User32Native.MouseEventRightUp, 0),
            MouseButton.Middle => (User32Native.MouseEventMiddleDown, User32Native.MouseEventMiddleUp, 0),
            MouseButton.Back => (User32Native.MouseEventXDown, User32Native.MouseEventXUp, User32Native.XButton1),
            MouseButton.Forward => (User32Native.MouseEventXDown, User32Native.MouseEventXUp, User32Native.XButton2),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unknown mouse button."),
        };

    /// <summary>The <c>KEYEVENTF_*</c> flags that press <paramref name="virtualKey"/>.</summary>
    internal static uint KeyDownFlags(ushort virtualKey) =>
        ExtendedKeys.Contains(virtualKey) ? User32Native.KeyEventExtendedKey : 0;

    private static NativeInput MouseEvent(uint flags, int dx, int dy, uint mouseData) =>
        new()
        {
            Type = User32Native.InputMouse,
            Data = new NativeInputUnion
            {
                Mouse = new NativeMouseInput { DeltaX = dx, DeltaY = dy, MouseData = mouseData, Flags = flags },
            },
        };

    /// <summary>
    /// Sends both the virtual key and its scan code: applications that read scan codes (many games, remote
    /// desktops) ignore events whose scan code is 0.
    /// </summary>
    private static NativeInput KeyEvent(ushort virtualKey, uint flags) =>
        new()
        {
            Type = User32Native.InputKeyboard,
            Data = new NativeInputUnion
            {
                Keyboard = new NativeKeyboardInput
                {
                    VirtualKey = virtualKey,
                    ScanCode = (ushort)User32Native.MapVirtualKey(virtualKey, User32Native.MapVirtualKeyToScanCode),
                    Flags = flags,
                },
            },
        };

    private static unsafe void Send(ReadOnlySpan<NativeInput> inputs)
    {
        if (inputs.IsEmpty)
        {
            return;
        }

        uint inserted;
        fixed (NativeInput* first = inputs)
        {
            inserted = User32Native.SendInput((uint)inputs.Length, first, sizeof(NativeInput));
        }

        if (inserted != inputs.Length)
        {
            Trace.TraceWarning(
                $"SendInput inserted {inserted} of {inputs.Length} events (error {Marshal.GetLastPInvokeError()}); "
                + "the foreground window may be elevated (UIPI).");
        }
    }
}
