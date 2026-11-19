using System.Diagnostics;
using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Output;
using ControllerPC.Core.Settings;
using ControllerPC.Windows.Timing;

namespace ControllerPC.App.Hosting;

/// <summary>
/// Runs a <see cref="ModeController"/> on a dedicated poll thread. Every member is called from the UI thread; commands
/// are queued and run on the poll thread, and every event is raised on the UI thread.
/// </summary>
/// <remarks>
/// The <see cref="ModeController"/> is only ever touched by the poll thread once <see cref="Start"/> has run, because it
/// is not thread-safe. Settings handed to <see cref="ApplySettings"/> belong to the poll thread from then on.
/// </remarks>
internal sealed class DesktopModeHost : IDisposable
{
    private const string PollThreadName = "Desktop Mode poll";
    private const uint TimerResolutionMilliseconds = 1;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(8);
    private static readonly TimeSpan ReleaseOutputTimeout = TimeSpan.FromSeconds(1);

    private readonly ModeController _controller;
    private readonly CommandQueue<TimeSpan> _commands = new();
    private readonly Stopwatch _clock = new();

    // Wakes the poll thread early when a command or stop arrives, so the UI never waits a whole poll interval.
    private readonly AutoResetEvent _wake = new(initialState: false);

    private SynchronizationContext? _uiContext;
    private Thread? _pollThread;
    private volatile bool _stopRequested;

    // Poll thread only: the generation of the settings the controller runs with.
    private int _runningSettingsGeneration;

    public DesktopModeHost(IControllerSource source, IRumble rumble, IInputSink sink, AppSettings settings)
    {
        _controller = new ModeController(source, rumble, sink, settings);
        _controller.StateChanged += (_, _) => PostSnapshot();
    }

    /// <summary>Desktop Mode, the Profiles or the Active Profile changed. Raised on the UI thread.</summary>
    public event EventHandler<DesktopModeSnapshot>? StateChanged;

    /// <summary>A request to enter Desktop Mode was turned down; the argument says why. Raised on the UI thread.</summary>
    public event EventHandler<string>? EnterRefused;

    /// <summary>
    /// The poll loop failed and has stopped, after releasing everything held. Raised on the UI thread.
    /// </summary>
    public event EventHandler<Exception>? Faulted;

    /// <summary>
    /// Counts <see cref="ApplySettings"/> calls; a <see cref="DesktopModeSnapshot"/> carrying a smaller number was
    /// taken before the latest settings were applied.
    /// </summary>
    public int LatestSettingsGeneration { get; private set; }

    /// <summary>Starts the poll thread. Call on the UI thread, whose synchronization context receives the events.</summary>
    /// <exception cref="InvalidOperationException">No synchronization context, or already started.</exception>
    public void Start()
    {
        if (_pollThread is not null)
        {
            throw new InvalidOperationException("The Desktop Mode host is already running.");
        }

        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("Start must be called on the UI thread.");

        // The poll thread does not exist yet, so reading the controller here is still single-threaded.
        var initial = DesktopModeSnapshot.Capture(_controller.IsDesktopModeActive, _controller.Settings, LatestSettingsGeneration);
        _uiContext.Post(_ => StateChanged?.Invoke(this, initial), null);

        _clock.Start();
        _pollThread = new Thread(Run)
        {
            Name = PollThreadName,
            IsBackground = true,

            // Cursor movement stutters when the poll thread waits behind ordinary work.
            Priority = ThreadPriority.AboveNormal,
        };
        _pollThread.Start();
    }

    public void EnterDesktopMode() => Enqueue(now =>
    {
        if (!_controller.EnterDesktopMode(now) && !_controller.IsDesktopModeActive)
        {
            var reason = _controller.IsPaused
                ? "Desktop Mode cannot be entered while a Mode Trigger is being recorded."
                : "Desktop Mode cannot be entered: no controller is connected.";
            Post(() => EnterRefused?.Invoke(this, reason));
        }
    });

    public void ExitDesktopMode() => Enqueue(now => _controller.ExitDesktopMode(now));

    public void SetActiveProfile(Guid profileId) => Enqueue(now => _controller.SetActiveProfile(profileId, now));

    /// <summary>Switches to <paramref name="settings"/>, which the caller must not touch afterwards.</summary>
    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var generation = ++LatestSettingsGeneration;
        Enqueue(now =>
        {
            _runningSettingsGeneration = generation;
            _controller.ApplySettings(settings, now);
        });
    }

    /// <summary>Releases everything held and stops matching and translating until <see cref="Resume"/>.</summary>
    public void Pause() => Enqueue(_ => _controller.Pause());

    public void Resume() => Enqueue(now => _controller.Resume(now));

    /// <summary>
    /// Leaves Desktop Mode, releasing every key and mouse button, and waits (briefly) until that is done. For
    /// unexpected errors, before anything else happens; may be called from any thread.
    /// </summary>
    public void ReleaseOutputNow()
    {
        if (Thread.CurrentThread == _pollThread)
        {
            _controller.ExitDesktopMode(_clock.Elapsed);
            return;
        }

        if (_pollThread is not { IsAlive: true })
        {
            return;
        }

        // Not disposed: if the wait times out, the command may still set it later.
        var released = new ManualResetEventSlim();
        Enqueue(now =>
        {
            try
            {
                _controller.ExitDesktopMode(now);
            }
            finally
            {
                released.Set();
            }
        });
        released.Wait(ReleaseOutputTimeout);
    }

    /// <summary>Leaves Desktop Mode (releasing everything held) and ends the poll thread. Safe to call twice.</summary>
    public void Stop()
    {
        if (_pollThread is not { } pollThread)
        {
            return;
        }

        _stopRequested = true;
        _wake.Set();
        pollThread.Join();
        _pollThread = null;
    }

    public void Dispose()
    {
        Stop();
        _wake.Dispose();
    }

    private void Run()
    {
        using var timerResolution = TimerResolution.Request(TimerResolutionMilliseconds);
        try
        {
            while (!_stopRequested)
            {
                var now = _clock.Elapsed;
                _commands.RunPending(now);
                _controller.Tick(now);
                _wake.WaitOne(PollInterval);
            }

            _controller.ExitDesktopMode(_clock.Elapsed);
        }
        catch (Exception exception)
        {
            // Nothing may stay pressed once the loop is gone; the UI decides what to tell the user.
            ReleaseAfterFault();
            Post(() => Faulted?.Invoke(this, exception));
        }
    }

    private void ReleaseAfterFault()
    {
        try
        {
            _controller.ExitDesktopMode(_clock.Elapsed);
        }
        catch (Exception exception)
        {
            Trace.TraceError($"Releasing output after a Desktop Mode failure failed too: {exception}");
        }
    }

    private void Enqueue(Action<TimeSpan> command)
    {
        _commands.Enqueue(command);
        _wake.Set();
    }

    // Raised by the controller on the poll thread (or, before Start, on the UI thread).
    private void PostSnapshot()
    {
        var snapshot = DesktopModeSnapshot.Capture(
            _controller.IsDesktopModeActive, _controller.Settings, _runningSettingsGeneration);
        Post(() => StateChanged?.Invoke(this, snapshot));
    }

    private void Post(Action action) => _uiContext?.Post(_ => action(), null);
}
