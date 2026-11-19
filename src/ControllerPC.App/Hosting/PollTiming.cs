using ControllerPC.Windows.Timing;

namespace ControllerPC.App.Hosting;

/// <summary>
/// How often and how eagerly the poll thread runs. In Desktop Mode it moves the cursor, so it polls fast, with a
/// fine system timer resolution and above-normal priority. Outside it only watches for Entry Triggers, and as the app
/// runs all day it polls more slowly with the default resolution and priority, to cost little power.
/// </summary>
/// <remarks>Belongs to the poll thread: it changes the priority of the thread that calls <see cref="Update"/>.</remarks>
internal sealed class PollTiming : IDisposable
{
    private const uint DesktopModeTimerResolutionMilliseconds = 1;
    private static readonly TimeSpan DesktopModeInterval = TimeSpan.FromMilliseconds(8);
    private static readonly TimeSpan WatchingInterval = TimeSpan.FromMilliseconds(16);

    private TimerResolution? _desktopModeResolution;

    /// <summary>How long to wait before the next poll.</summary>
    public TimeSpan Interval => _desktopModeResolution is null ? WatchingInterval : DesktopModeInterval;

    /// <summary>Switches to Desktop Mode timing or back, when <paramref name="desktopModeActive"/> changed.</summary>
    public void Update(bool desktopModeActive)
    {
        if (desktopModeActive == _desktopModeResolution is not null)
        {
            return;
        }

        if (desktopModeActive)
        {
            _desktopModeResolution = TimerResolution.Request(DesktopModeTimerResolutionMilliseconds);

            // Cursor movement stutters when the poll thread waits behind ordinary work.
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
        }
        else
        {
            Dispose();
            Thread.CurrentThread.Priority = ThreadPriority.Normal;
        }
    }

    /// <summary>Gives the timer resolution back; call on the poll thread as it ends.</summary>
    public void Dispose()
    {
        _desktopModeResolution?.Dispose();
        _desktopModeResolution = null;
    }
}
