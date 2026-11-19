using System.Runtime.InteropServices;

namespace ControllerPC.Windows.XInput;

/// <summary>P/Invoke declarations for xinput1_4.dll (Windows 8 and later).</summary>
internal static partial class XInputNative
{
    /// <summary><c>XUSER_MAX_COUNT</c>: number of controller slots XInput supports.</summary>
    public const int MaxControllerCount = 4;

    /// <summary><c>XINPUT_GAMEPAD_TRIGGER_THRESHOLD</c>.</summary>
    public const byte TriggerThreshold = 30;

    /// <summary><c>ERROR_SUCCESS</c>.</summary>
    public const uint ErrorSuccess = 0;

    /// <summary><c>ERROR_DEVICE_NOT_CONNECTED</c>: nothing is plugged into the slot.</summary>
    public const uint ErrorDeviceNotConnected = 1167;

    private const string XInputLibrary = "xinput1_4.dll";

    /// <summary><c>XInputGetState</c>. Returns a Win32 error code.</summary>
    [LibraryImport(XInputLibrary)]
    public static partial uint XInputGetState(uint userIndex, out XInputState state);

    /// <summary><c>XInputSetState</c>. Returns a Win32 error code.</summary>
    [LibraryImport(XInputLibrary)]
    public static partial uint XInputSetState(uint userIndex, in XInputVibration vibration);
}
