using System.ComponentModel;
using System.Diagnostics;
using ControllerPC.App.Hosting;
using ControllerPC.App.UI;
using ControllerPC.Core.Settings;
using ControllerPC.Windows.Output;
using ControllerPC.Windows.XInput;
using Microsoft.Win32;

namespace ControllerPC.App;

/// <summary>
/// The running tray app: wires the tray icon, the settings window and the Desktop Mode host together, and owns the
/// saved settings. Everything here runs on the UI thread, including every settings save.
/// </summary>
internal sealed class ControllerPcContext : ApplicationContext
{
    private const string Title = "ControllerPC";

    /// <summary><c>ERROR_CANCELLED</c>: the user declined the UAC prompt.</summary>
    private const int ErrorCancelled = 1223;

    private readonly SettingsStore _store;
    private readonly SingleInstanceLock _instanceLock;
    private readonly XInputControllerSource _controllerSource = new();
    private readonly XInputRumble _rumble = new();
    private readonly DesktopModeHost _host;
    private readonly TrayIcon _tray;

    // The settings as saved; the host runs its own copy.
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private bool _shutDown;

    /// <param name="settings">The loaded settings; this context owns them from now on.</param>
    /// <param name="store">Where settings are saved.</param>
    /// <param name="instanceLock">Held by this instance; let go briefly when restarting as administrator.</param>
    public ControllerPcContext(AppSettings settings, SettingsStore store, SingleInstanceLock instanceLock)
    {
        _settings = settings;
        _store = store;
        _instanceLock = instanceLock;

        // Created first: its menu is the first control, which installs the UI synchronization context the host needs.
        _tray = new TrayIcon();
        _host = new DesktopModeHost(_controllerSource, _rumble, new SendInputSink(), SettingsSerializer.Clone(settings));

        _tray.EnterRequested += (_, _) => _host.EnterDesktopMode();
        _tray.ExitRequested += (_, _) => _host.ExitDesktopMode();
        _tray.ProfileSelected += (_, profileId) => _host.SetActiveProfile(profileId);
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.RestartAsAdminRequested += (_, _) => RestartAsAdministrator();
        _tray.QuitRequested += (_, _) => Quit();

        _host.StateChanged += (_, snapshot) => OnStateChanged(snapshot);
        _host.EnterRefused += (_, reason) =>
        {
            if (!_shutDown)
            {
                _tray.ShowNotice(reason);
            }
        };
        _host.Faulted += (_, exception) => OnHostFaulted(exception);

        // Windows may end the process soon after the session ends, so release output as early as possible.
        SystemEvents.SessionEnding += OnSessionEnding;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        Application.ApplicationExit += OnApplicationExit;
        _host.Start();
    }

    /// <summary>
    /// For the global exception handlers: releases every key and mouse button first, then tells the user.
    /// May be called from any thread.
    /// </summary>
    /// <param name="exception">The unhandled exception, when known.</param>
    /// <param name="fatal">True when the process is about to end.</param>
    public void ReportUnexpectedError(Exception? exception, bool fatal)
    {
        _host.ReleaseOutputNow();
        var outcome = fatal ? "ControllerPC has to close." : "Desktop Mode was switched off.";
        MessageBox.Show(
            $"An unexpected error occurred. {outcome}\n\n{exception?.Message}",
            Title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ShutDown();
            _host.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnSessionEnding(object? sender, SessionEndingEventArgs e) => _host.ReleaseOutputNow();

    // Nobody is at this desktop any more: a controller left in Desktop Mode would keep driving the locked session.
    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)
        {
            _host.ExitDesktopMode();
        }
    }

    private void OnApplicationExit(object? sender, EventArgs e) => ShutDown();

    private void OnStateChanged(DesktopModeSnapshot snapshot)
    {
        if (_shutDown)
        {
            return;
        }

        _tray.ShowState(snapshot.DesktopModeActive, snapshot.Profiles, snapshot.ActiveProfileId);
        if (ActiveProfileTracking.ShouldPersist(snapshot, _host.LatestSettingsGeneration, _settings))
        {
            _settings.ActiveProfileId = snapshot.ActiveProfileId;
            SaveActiveProfile();
        }
    }

    // A Profile switch is not worth interrupting the user for; it stays in effect until the app quits.
    private void SaveActiveProfile()
    {
        try
        {
            _store.Save(_settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Saving the Active Profile failed: {exception.Message}");
        }
    }

    private void OpenSettings()
    {
        if (_settingsForm is { } open)
        {
            if (open.WindowState == FormWindowState.Minimized)
            {
                open.WindowState = FormWindowState.Normal;
            }

            open.Activate();
            return;
        }

        var form = new SettingsForm(SettingsSerializer.Clone(_settings), _controllerSource);
        form.SettingsApplied += (_, edited) => ApplyEditedSettings(form, edited);
        form.RecordingStarted += (_, _) => _host.Pause();
        form.RecordingFinished += (_, _) => _host.Resume();

        // A modeless form disposes itself when closed.
        form.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm = form;
        form.Show();
    }

    private void ApplyEditedSettings(IWin32Window owner, AppSettings edited)
    {
        ActiveProfileTracking.CarryOver(edited, _settings.ActiveProfileId);
        try
        {
            _store.Save(edited);
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(owner, $"The settings were not applied:\n\n{exception.Message}", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                owner,
                $"The settings could not be saved:\n{exception.Message}\n\nThey apply until ControllerPC quits.\n\n"
                + $"Settings file:\n{_store.FilePath}",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _settings = edited;
        _host.ApplySettings(SettingsSerializer.Clone(edited));
    }

    private void OnHostFaulted(Exception exception)
    {
        if (_shutDown)
        {
            return;
        }

        MessageBox.Show(
            $"Desktop Mode stopped because of an unexpected error, so ControllerPC will close.\n\n{exception.Message}",
            Title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        Quit();
    }

    /// <summary>
    /// Starts this app again elevated, so it can drive administrator windows, and quits. The single-instance lock
    /// is let go first so the new instance can take it, and taken back if the user cancels the UAC prompt.
    /// </summary>
    private void RestartAsAdministrator()
    {
        if (Environment.ProcessPath is not { } executablePath)
        {
            MessageBox.Show("ControllerPC could not find its own program file to restart.", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Nothing stays pressed while the two instances briefly overlap.
        _host.Pause();
        _instanceLock.Release();
        try
        {
            using var elevated = Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true, Verb = "runas" });
        }
        catch (Win32Exception exception)
        {
            KeepRunningAfterFailedRestart(exception);
            return;
        }

        Quit();
    }

    private void KeepRunningAfterFailedRestart(Win32Exception exception)
    {
        if (!_instanceLock.TryReacquire())
        {
            // Another instance started in the gap and now owns the lock: let it be the one that runs.
            Quit();
            return;
        }

        _host.Resume();
        if (exception.NativeErrorCode != ErrorCancelled)
        {
            MessageBox.Show($"ControllerPC could not restart as administrator:\n{exception.Message}", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Quit()
    {
        ShutDown();
        ExitThread();
    }

    // Order matters: the poll loop releases everything and stops before the rumble it uses is disposed.
    private void ShutDown()
    {
        if (_shutDown)
        {
            return;
        }

        _shutDown = true;
        SystemEvents.SessionEnding -= OnSessionEnding;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        Application.ApplicationExit -= OnApplicationExit;
        _settingsForm?.Close();
        _host.Stop();
        _rumble.Dispose();
        _tray.Dispose();
    }
}
