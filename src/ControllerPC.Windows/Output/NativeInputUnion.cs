using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Output;

/// <summary>The anonymous union inside <c>INPUT</c>; its size is that of <c>MOUSEINPUT</c>, the largest member.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct NativeInputUnion
{
    [FieldOffset(0)]
    public NativeMouseInput Mouse;

    [FieldOffset(0)]
    public NativeKeyboardInput Keyboard;

    /// <summary>Never sent; declared so the union mirrors the Win32 definition member for member.</summary>
    [FieldOffset(0)]
    public NativeHardwareInput Hardware;
}
