namespace ControllerPC.App.UI;

/// <summary>The app icon (the controller glyph), embedded in the assembly at every size the .ico holds.</summary>
internal static class AppIcon
{
    private const string ResourceName = "ControllerPC.ico";

    /// <summary>
    /// One instance shared by every window for the whole life of the app, so it is never disposed. Forms keep their
    /// own copies for the sizes they show.
    /// </summary>
    public static Icon Shared { get; } = Load();

    private static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource \"{ResourceName}\" is missing.");
        return new Icon(stream);
    }
}
