using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ControllerPC.App.UI;

namespace ControllerPC.IconGenerator;

/// <summary>
/// Writes the app icon from <see cref="ControllerGlyph"/>, and optionally PNG previews to check it by eye.
/// </summary>
/// <example>
/// <code>
/// dotnet run --project tools/ControllerPC.IconGenerator -- src/ControllerPC.App/Assets/ControllerPC.ico [previewFolder]
/// </code>
/// </example>
internal static class Program
{
    private const int Success = 0;
    private const int UsageError = 1;
    private const int PreviewZoom = 8;
    private const int PreviewZoomMaximumSize = 48;

    private static readonly int[] IconSizes = [16, 20, 24, 32, 40, 48, 64, 256];
    private static readonly int[] PreviewSizes = [16, 32, 48, 256];

    // Typical light and dark taskbar colors, to check the glyph stands out on both.
    private static readonly (string Name, Color Color)[] PreviewBackgrounds =
        [("light", Color.FromArgb(0xF3, 0xF3, 0xF3)), ("dark", Color.FromArgb(0x20, 0x20, 0x20))];

    private static int Main(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("Usage: ControllerPC.IconGenerator <output.ico> [previewFolder]");
            return UsageError;
        }

        WriteIcon(args[0]);
        if (args.Length == 2)
        {
            WritePreviews(args[1]);
        }

        return Success;
    }

    private static void WriteIcon(string path)
    {
        var images = IconSizes.Select(size => ControllerGlyph.Render(size, ControllerGlyphColors.Neutral)).ToList();
        try
        {
            using var file = File.Create(path);
            IconFile.Write(file, images);
        }
        finally
        {
            images.ForEach(image => image.Dispose());
        }

        Console.WriteLine($"Wrote {path}");
    }

    private static void WritePreviews(string folder)
    {
        Directory.CreateDirectory(folder);
        (string Name, ControllerGlyphColors Colors)[] variants =
            [("neutral", ControllerGlyphColors.Neutral), ("on", ControllerGlyphColors.DesktopModeOn)];

        foreach (var (variantName, colors) in variants)
        {
            foreach (var size in PreviewSizes)
            {
                using var glyph = ControllerGlyph.Render(size, colors);
                foreach (var (backgroundName, background) in PreviewBackgrounds)
                {
                    // Small sizes are zoomed with hard pixel edges so single pixels can be judged.
                    var zoom = size <= PreviewZoomMaximumSize ? PreviewZoom : 1;
                    using var preview = OnBackground(glyph, background, zoom);
                    var path = Path.Combine(folder, $"{variantName}-{size}-{backgroundName}.png");
                    preview.Save(path, ImageFormat.Png);
                    Console.WriteLine($"Wrote {path}");
                }
            }
        }
    }

    private static Bitmap OnBackground(Bitmap glyph, Color background, int zoom)
    {
        var preview = new Bitmap(glyph.Width * zoom, glyph.Height * zoom, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(preview);
        graphics.Clear(background);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(glyph, 0, 0, preview.Width, preview.Height);
        return preview;
    }
}
