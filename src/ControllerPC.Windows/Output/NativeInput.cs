using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Output;

/// <summary>
/// <c>INPUT</c>. Sequential layout lets the runtime align <see cref="Data"/> to its largest member's alignment
/// (pointer-sized <c>dwExtraInfo</c>), giving the Win32 layout on both architectures: on x64 the union starts at
/// offset 8 and the struct is 40 bytes (4 type + 4 padding + 32 <c>MOUSEINPUT</c>); on x86 it is 28 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeInput
{
    /// <summary><c>type</c>: <see cref="User32Native.InputMouse"/> or <see cref="User32Native.InputKeyboard"/>.</summary>
    public uint Type;

    public NativeInputUnion Data;
}
