using System.Diagnostics;

namespace ControllerPC.Windows.Timing;

/// <summary>
/// Raises the system timer resolution while it is alive, so sleeps and timed waits of a few milliseconds wake on
/// time instead of up to ~15.6 ms late. Dispose it as soon as precise timing is no longer needed: a finer
/// resolution costs power for the whole system.
/// </summary>
public sealed class TimerResolution : IDisposable
{
    private readonly uint _periodMilliseconds;
    private bool _active;

    private TimerResolution(uint periodMilliseconds, bool active)
    {
        _periodMilliseconds = periodMilliseconds;
        _active = active;
    }

    /// <summary>
    /// Requests a timer resolution of <paramref name="periodMilliseconds"/>. When Windows refuses, the request is
    /// logged and the returned instance does nothing: timing gets coarser, which is not worth failing over.
    /// </summary>
    public static TimerResolution Request(uint periodMilliseconds)
    {
        uint result = WinmmNative.TimeBeginPeriod(periodMilliseconds);
        bool active = result == WinmmNative.TimerNoError;
        if (!active)
        {
            Trace.TraceWarning($"timeBeginPeriod({periodMilliseconds}) failed with result {result}.");
        }

        return new TimerResolution(periodMilliseconds, active);
    }

    public void Dispose()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        uint result = WinmmNative.TimeEndPeriod(_periodMilliseconds);
        if (result != WinmmNative.TimerNoError)
        {
            Trace.TraceWarning($"timeEndPeriod({_periodMilliseconds}) failed with result {result}.");
        }
    }
}
