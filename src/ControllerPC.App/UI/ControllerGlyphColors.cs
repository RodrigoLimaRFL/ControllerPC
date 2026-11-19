namespace ControllerPC.App.UI;

/// <summary>The colors of a <see cref="ControllerGlyph"/>.</summary>
/// <param name="Body">The controller body and grips.</param>
/// <param name="Edge">
/// The outline around the body. Lighter than the body, so the glyph stands out on a dark taskbar as well as a light one.
/// </param>
/// <param name="Details">The D-pad and face buttons.</param>
internal sealed record ControllerGlyphColors(Color Body, Color Edge, Color Details)
{
    /// <summary>The app icon, and the tray icon outside Desktop Mode.</summary>
    public static ControllerGlyphColors Neutral { get; } =
        new(Color.FromArgb(0x44, 0x44, 0x44), Color.FromArgb(0xB4, 0xB4, 0xB4), Color.FromArgb(0xF4, 0xF4, 0xF4));

    /// <summary>The tray icon while Desktop Mode is on.</summary>
    public static ControllerGlyphColors DesktopModeOn { get; } =
        new(Color.FromArgb(0x1E, 0x9E, 0x3A), Color.FromArgb(0x9C, 0xE6, 0xAC), Color.White);
}
