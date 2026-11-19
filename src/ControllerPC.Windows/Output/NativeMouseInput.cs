using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Output;

/// <summary><c>MOUSEINPUT</c> (32 bytes on x64, 24 on x86).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeMouseInput
{
    /// <summary><c>dx</c>: relative horizontal movement when <c>MOUSEEVENTF_MOVE</c> is set.</summary>
    public int DeltaX;

    /// <summary><c>dy</c>: relative vertical movement (positive is down) when <c>MOUSEEVENTF_MOVE</c> is set.</summary>
    public int DeltaY;

    /// <summary><c>mouseData</c>: wheel delta or X button, depending on <see cref="Flags"/>.</summary>
    public uint MouseData;

    /// <summary><c>dwFlags</c>: <c>MOUSEEVENTF_*</c>.</summary>
    public uint Flags;

    /// <summary><c>time</c>: 0 lets the system supply the timestamp.</summary>
    public uint Time;

    /// <summary><c>dwExtraInfo</c> (<c>ULONG_PTR</c>).</summary>
    public nuint ExtraInfo;
}
