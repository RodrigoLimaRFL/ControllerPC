using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Output;

/// <summary><c>KEYBDINPUT</c> (24 bytes on x64, 16 on x86).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeKeyboardInput
{
    /// <summary><c>wVk</c>: virtual-key code.</summary>
    public ushort VirtualKey;

    /// <summary><c>wScan</c>: hardware scan code, without the 0xE0 prefix.</summary>
    public ushort ScanCode;

    /// <summary><c>dwFlags</c>: <c>KEYEVENTF_*</c>.</summary>
    public uint Flags;

    /// <summary><c>time</c>: 0 lets the system supply the timestamp.</summary>
    public uint Time;

    /// <summary><c>dwExtraInfo</c> (<c>ULONG_PTR</c>).</summary>
    public nuint ExtraInfo;
}
