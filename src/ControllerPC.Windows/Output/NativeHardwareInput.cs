using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Output;

/// <summary><c>HARDWAREINPUT</c> (8 bytes).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeHardwareInput
{
    /// <summary><c>uMsg</c>.</summary>
    public uint Message;

    /// <summary><c>wParamL</c>.</summary>
    public ushort ParamLow;

    /// <summary><c>wParamH</c>.</summary>
    public ushort ParamHigh;
}
