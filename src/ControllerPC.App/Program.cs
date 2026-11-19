using ControllerPC.Core.Settings;

namespace ControllerPC.App;

internal static class Program
{
    /// <summary>Per user session: another user signed in on the same PC may run their own instance.</summary>
    private const string SingleInstanceName = @"Local\ControllerPC";

    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        ApplicationConfiguration.Initialize();

        using var instanceLock = SingleInstanceLock.TryAcquire(SingleInstanceName);
        if (instanceLock is null)
        {
            MessageBox.Show(
                "ControllerPC is already running. Use its icon in the notification area.",
                "ControllerPC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var store = new SettingsStore(SettingsStore.DefaultFilePath);
        var settings = StartupSettings.Load(store);
        using var context = new ControllerPcContext(settings, store, instanceLock);
        Application.ThreadException += (_, e) => context.ReportUnexpectedError(e.Exception, fatal: false);
        AppDomain.CurrentDomain.UnhandledException +=
            (_, e) => context.ReportUnexpectedError(e.ExceptionObject as Exception, e.IsTerminating);
        Application.Run(context);
    }
}
