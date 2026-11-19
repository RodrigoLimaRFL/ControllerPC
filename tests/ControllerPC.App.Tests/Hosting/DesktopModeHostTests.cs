using ControllerPC.App.Hosting;
using ControllerPC.App.Tests.Fakes;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.Tests.Hosting;

/// <summary>
/// Runs the host's real poll thread against fakes; the test thread plays the UI thread. Waits are bounded by
/// <see cref="Timeout"/> and end as soon as the expected event arrives.
/// </summary>
public sealed class DesktopModeHostTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly SynchronizationContext? _previousContext = SynchronizationContext.Current;
    private readonly QueueSynchronizationContext _ui = new();
    private readonly SettableControllerSource _source = new();
    private readonly DesktopModeHost _host;
    private readonly List<DesktopModeSnapshot> _snapshots = [];
    private readonly List<string> _refusals = [];

    public DesktopModeHostTests()
    {
        SynchronizationContext.SetSynchronizationContext(_ui);
        _host = new DesktopModeHost(_source, new SilentRumble(), new SilentInputSink(), DefaultSettings.Create());
        _host.StateChanged += (_, snapshot) => _snapshots.Add(snapshot);
        _host.EnterRefused += (_, reason) => _refusals.Add(reason);
        _host.Start();
    }

    [Fact]
    public void Start_ReportsTheInitialState()
    {
        Assert.True(_ui.RunUntil(() => _snapshots.Count > 0, Timeout));
        Assert.False(_snapshots[0].DesktopModeActive);
    }

    [Fact]
    public void EnterDesktopMode_ReportsDesktopModeOnWhenAControllerIsConnected()
    {
        _source.Connect(slot: 0);

        _host.EnterDesktopMode();

        Assert.True(_ui.RunUntil(IsDesktopModeOn, Timeout));
    }

    [Fact]
    public void EnterDesktopMode_IsRefusedWithoutAController()
    {
        _host.EnterDesktopMode();

        Assert.True(_ui.RunUntil(() => _refusals.Count > 0, Timeout));
    }

    [Fact]
    public void Stop_LeavesDesktopMode()
    {
        _source.Connect(slot: 0);
        _host.EnterDesktopMode();
        _ui.RunUntil(IsDesktopModeOn, Timeout);

        _host.Stop();

        Assert.True(_ui.RunUntil(() => !IsDesktopModeOn(), Timeout));
    }

    [Fact]
    public void Resume_KeepsThePauseWhileAnotherPauseIsOpen()
    {
        _source.Connect(slot: 0);
        _host.Pause();
        _host.Pause();
        _host.Resume();

        _host.EnterDesktopMode();

        Assert.True(_ui.RunUntil(() => _refusals.Count > 0, Timeout));
    }

    [Fact]
    public void Resume_EndsThePauseWhenEveryPauseIsMatched()
    {
        _source.Connect(slot: 0);
        _host.Pause();
        _host.Pause();
        _host.Resume();
        _host.Resume();

        _host.EnterDesktopMode();

        Assert.True(_ui.RunUntil(IsDesktopModeOn, Timeout));
    }

    public void Dispose()
    {
        _host.Dispose();
        _ui.Dispose();
        SynchronizationContext.SetSynchronizationContext(_previousContext);
    }

    private bool IsDesktopModeOn() => _snapshots.Count > 0 && _snapshots[^1].DesktopModeActive;
}
