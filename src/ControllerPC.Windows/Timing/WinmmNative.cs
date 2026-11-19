using System.Runtime.InteropServices;

namespace ControllerPC.Windows.Timing;

/// <summary>P/Invoke declarations for winmm.dll timer resolution.</summary>
internal static partial class WinmmNative
{
    /// <summary><c>TIMERR_NOERROR</c>.</summary>
    public const uint TimerNoError = 0;

    private const string WinmmLibrary = "winmm.dll";

    /// <summary><c>timeBeginPeriod</c>. Returns <see cref="TimerNoError"/> or <c>TIMERR_NOCANDO</c>.</summary>
    [LibraryImport(WinmmLibrary, EntryPoint = "timeBeginPeriod")]
    public static partial uint TimeBeginPeriod(uint periodMilliseconds);

    /// <summary><c>timeEndPeriod</c>. Must be called once for every successful <see cref="TimeBeginPeriod"/>.</summary>
    [LibraryImport(WinmmLibrary, EntryPoint = "timeEndPeriod")]
    public static partial uint TimeEndPeriod(uint periodMilliseconds);
}
