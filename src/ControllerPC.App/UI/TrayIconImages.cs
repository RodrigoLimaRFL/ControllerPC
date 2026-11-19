namespace ControllerPC.App.UI;

/// <summary>
/// Draws the two tray icons, the controller glyph in gray (Desktop Mode off) or green (on), at the system
/// small-icon size for the current DPI.
/// </summary>
/// <remarks>
/// The bitmap is wrapped in an in-memory .ico container rather than converted with <c>Bitmap.GetHicon</c>, so the
/// resulting <see cref="Icon"/> owns its handle and a plain <c>Dispose</c> releases it.
/// </remarks>
internal static class TrayIconImages
{
    /// <summary>Creates a new icon; the caller owns and disposes it.</summary>
    public static Icon Create(bool desktopModeActive)
    {
        var colors = desktopModeActive ? ControllerGlyphColors.DesktopModeOn : ControllerGlyphColors.Neutral;
        using var bitmap = ControllerGlyph.Render(SystemInformation.SmallIconSize.Width, colors);
        return IconFile.ToIcon(bitmap);
    }
}
