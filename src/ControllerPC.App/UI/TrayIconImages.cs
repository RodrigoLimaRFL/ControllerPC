using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ControllerPC.App.UI;

/// <summary>
/// Draws the two tray icons (Desktop Mode off / on) at the system small-icon size.
/// </summary>
/// <remarks>
/// The bitmap is wrapped in an in-memory .ico container rather than converted with <c>Bitmap.GetHicon</c>, so the
/// resulting <see cref="Icon"/> owns its handle and a plain <c>Dispose</c> releases it.
/// </remarks>
internal static class TrayIconImages
{
    private static readonly Color OffFill = Color.Silver;
    private static readonly Color OnFill = Color.LimeGreen;
    private static readonly Color Outline = Color.Black;

    // .ico layout: a 6-byte ICONDIR header, one 16-byte ICONDIRENTRY, then the PNG image data.
    private const short Reserved = 0;
    private const short IconFileType = 1;
    private const short ImageCount = 1;
    private const byte NoPalette = 0;
    private const short ColorPlanes = 1;
    private const short BitsPerPixel = 32;
    private const int IconDirectorySize = 6;
    private const int IconDirectoryEntrySize = 16;

    /// <summary>Creates a new icon; the caller owns and disposes it.</summary>
    public static Icon Create(bool desktopModeActive)
    {
        var size = SystemInformation.SmallIconSize;
        using var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            Draw(graphics, size, desktopModeActive ? OnFill : OffFill);
        }

        return ToIcon(bitmap);
    }

    private static void Draw(Graphics graphics, Size size, Color fill)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(1, 1, size.Width - 3, size.Height - 3);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(Outline);
        graphics.FillEllipse(brush, bounds);
        graphics.DrawEllipse(pen, bounds);
    }

    private static Icon ToIcon(Bitmap bitmap)
    {
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);

        using var ico = new MemoryStream();
        using (var writer = new BinaryWriter(ico, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Reserved);
            writer.Write(IconFileType);
            writer.Write(ImageCount);

            // A width or height of 256 is stored as 0, which the byte cast produces.
            writer.Write((byte)bitmap.Width);
            writer.Write((byte)bitmap.Height);
            writer.Write(NoPalette);
            writer.Write((byte)Reserved);
            writer.Write(ColorPlanes);
            writer.Write(BitsPerPixel);
            writer.Write((int)png.Length);
            writer.Write(IconDirectorySize + IconDirectoryEntrySize);
            writer.Write(png.ToArray());
        }

        ico.Position = 0;
        return new Icon(ico, bitmap.Size);
    }
}
