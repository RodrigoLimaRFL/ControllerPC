using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Output;

/// <summary>P/Invoke declarations and constants from user32.dll used to synthesize input.</summary>
internal static partial class User32Native
{
    /// <summary><c>INPUT_MOUSE</c>.</summary>
    public const uint InputMouse = 0;

    /// <summary><c>INPUT_KEYBOARD</c>.</summary>
    public const uint InputKeyboard = 1;

    /// <summary><c>MOUSEEVENTF_MOVE</c>: relative movement (no <c>MOUSEEVENTF_ABSOLUTE</c>).</summary>
    public const uint MouseEventMove = 0x0001;

    /// <summary><c>MOUSEEVENTF_LEFTDOWN</c>.</summary>
    public const uint MouseEventLeftDown = 0x0002;

    /// <summary><c>MOUSEEVENTF_LEFTUP</c>.</summary>
    public const uint MouseEventLeftUp = 0x0004;

    /// <summary><c>MOUSEEVENTF_RIGHTDOWN</c>.</summary>
    public const uint MouseEventRightDown = 0x0008;

    /// <summary><c>MOUSEEVENTF_RIGHTUP</c>.</summary>
    public const uint MouseEventRightUp = 0x0010;

    /// <summary><c>MOUSEEVENTF_MIDDLEDOWN</c>.</summary>
    public const uint MouseEventMiddleDown = 0x0020;

    /// <summary><c>MOUSEEVENTF_MIDDLEUP</c>.</summary>
    public const uint MouseEventMiddleUp = 0x0040;

    /// <summary><c>MOUSEEVENTF_XDOWN</c>: which X button is given in <c>mouseData</c>.</summary>
    public const uint MouseEventXDown = 0x0080;

    /// <summary><c>MOUSEEVENTF_XUP</c>: which X button is given in <c>mouseData</c>.</summary>
    public const uint MouseEventXUp = 0x0100;

    /// <summary><c>MOUSEEVENTF_WHEEL</c>: vertical wheel delta in <c>mouseData</c>, positive is away from the user.</summary>
    public const uint MouseEventWheel = 0x0800;

    /// <summary><c>MOUSEEVENTF_HWHEEL</c>: horizontal wheel delta in <c>mouseData</c>, positive is right.</summary>
    public const uint MouseEventHorizontalWheel = 0x1000;

    /// <summary><c>XBUTTON1</c>: the side button conventionally used for Back.</summary>
    public const uint XButton1 = 0x0001;

    /// <summary><c>XBUTTON2</c>: the side button conventionally used for Forward.</summary>
    public const uint XButton2 = 0x0002;

    /// <summary><c>KEYEVENTF_EXTENDEDKEY</c>: the scan code is preceded by the 0xE0 prefix.</summary>
    public const uint KeyEventExtendedKey = 0x0001;

    /// <summary><c>KEYEVENTF_KEYUP</c>.</summary>
    public const uint KeyEventKeyUp = 0x0002;

    /// <summary><c>MAPVK_VK_TO_VSC</c>.</summary>
    public const uint MapVirtualKeyToScanCode = 0;

    private const string User32Library = "user32.dll";

    /// <summary>
    /// <c>SendInput</c>. Returns the number of events inserted; 0 when blocked by another thread or by UIPI
    /// (input into a higher-integrity window), in which case the last error does not say why.
    /// </summary>
    [LibraryImport(User32Library, SetLastError = true)]
    public static unsafe partial uint SendInput(uint inputCount, NativeInput* inputs, int inputSize);

    /// <summary><c>MapVirtualKeyW</c>. Returns 0 when there is no translation.</summary>
    [LibraryImport(User32Library, EntryPoint = "MapVirtualKeyW")]
    public static partial uint MapVirtualKey(uint code, uint mapType);
}
