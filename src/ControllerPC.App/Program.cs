namespace ControllerPC.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Wiring of tray, settings window and Desktop Mode loop lands in feature/app-wiring.
    }
}
