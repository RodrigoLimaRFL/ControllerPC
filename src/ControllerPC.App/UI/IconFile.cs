using System.Drawing.Imaging;

namespace ControllerPC.App.UI;

/// <summary>
/// Writes square 32-bit images as a Windows .ico file. Images up to 255 pixels are stored as uncompressed DIBs, which
/// every icon reader understands; 256-pixel images as PNG, as Windows expects for that size.
/// </summary>
internal static class IconFile
{
    // ICONDIR: reserved, type, count. ICONDIRENTRY: 16 bytes each. BITMAPINFOHEADER: 40 bytes.
    private const short Reserved = 0;
    private const short IconType = 1;
    private const int DirectorySize = 6;
    private const int DirectoryEntrySize = 16;
    private const int BitmapInfoHeaderSize = 40;
    private const short ColorPlanes = 1;
    private const short BitsPerPixel = 32;
    private const int BytesPerPixel = 4;
    private const int NoCompression = 0;
    private const int UnusedField = 0;
    private const byte NoPalette = 0;
    private const int PngMinimumSize = 256;
    private const int MaskRowAlignmentBits = 32;

    /// <summary>Writes <paramref name="images"/> (square, 32 bpp ARGB, at most 256 pixels) to <paramref name="output"/>.</summary>
    public static void Write(Stream output, IReadOnlyList<Bitmap> images)
    {
        var encoded = images.Select(Encode).ToList();
        using var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(Reserved);
        writer.Write(IconType);
        writer.Write((short)images.Count);

        var offset = DirectorySize + (DirectoryEntrySize * images.Count);
        for (var i = 0; i < images.Count; i++)
        {
            // A width or height of 256 is stored as 0, which the byte cast produces.
            writer.Write((byte)images[i].Width);
            writer.Write((byte)images[i].Height);
            writer.Write(NoPalette);
            writer.Write((byte)Reserved);
            writer.Write(ColorPlanes);
            writer.Write(BitsPerPixel);
            writer.Write(encoded[i].Length);
            writer.Write(offset);
            offset += encoded[i].Length;
        }

        foreach (var image in encoded)
        {
            writer.Write(image);
        }
    }

    /// <summary>A new icon holding just <paramref name="image"/>; the caller disposes it.</summary>
    public static Icon ToIcon(Bitmap image)
    {
        using var stream = new MemoryStream();
        Write(stream, [image]);
        stream.Position = 0;
        return new Icon(stream, image.Size);
    }

    private static byte[] Encode(Bitmap image)
    {
        if (image.Width != image.Height || image.Width > PngMinimumSize)
        {
            throw new ArgumentException("Icon images must be square and at most 256 pixels.", nameof(image));
        }

        return image.Width >= PngMinimumSize ? EncodePng(image) : EncodeDib(image);
    }

    private static byte[] EncodePng(Bitmap image)
    {
        using var png = new MemoryStream();
        image.Save(png, ImageFormat.Png);
        return png.ToArray();
    }

    /// <summary>
    /// BITMAPINFOHEADER, then BGRA rows bottom-up, then the 1-bit AND mask (all zero: transparency comes from alpha).
    /// The header height counts both the image and the mask.
    /// </summary>
    private static byte[] EncodeDib(Bitmap image)
    {
        var size = image.Width;
        var maskRowBytes = ((size + MaskRowAlignmentBits - 1) / MaskRowAlignmentBits) * (MaskRowAlignmentBits / 8);
        var pixelBytes = size * size * BytesPerPixel;

        using var dib = new MemoryStream();
        using (var writer = new BinaryWriter(dib, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(BitmapInfoHeaderSize);
            writer.Write(size);
            writer.Write(2 * size);
            writer.Write(ColorPlanes);
            writer.Write(BitsPerPixel);
            writer.Write(NoCompression);
            writer.Write(pixelBytes);

            // Horizontal and vertical resolution, colors used, important colors: not used by icons.
            writer.Write(UnusedField);
            writer.Write(UnusedField);
            writer.Write(UnusedField);
            writer.Write(UnusedField);

            for (var y = size - 1; y >= 0; y--)
            {
                for (var x = 0; x < size; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    writer.Write(pixel.B);
                    writer.Write(pixel.G);
                    writer.Write(pixel.R);
                    writer.Write(pixel.A);
                }
            }

            writer.Write(new byte[maskRowBytes * size]);
        }

        return dib.ToArray();
    }
}
