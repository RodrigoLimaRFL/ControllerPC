using ControllerPC.App.UI;

namespace ControllerPC.App.Tests.UI;

public class IconFileTests
{
    private static readonly int[] Sizes = [16, 32, 256];

    private static readonly byte[] PngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G'];

    // System.Drawing.Icon ignores the PNG-compressed 256-pixel entry (Windows itself reads it), so these sizes are
    // checked through Icon and the 256 entry through the file layout.
    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    public void Write_StoresSmallImagesAtTheirSize(int size)
    {
        using var stream = WriteGlyphs();

        using var icon = new Icon(stream, size, size);

        Assert.Equal(new Size(size, size), icon.Size);
    }

    [Fact]
    public void Write_KeepsThePixels()
    {
        using var expected = ControllerGlyph.Render(16, ControllerGlyphColors.Neutral);
        using var stream = WriteGlyphs();

        using var icon = new Icon(stream, 16, 16);
        using var actual = icon.ToBitmap();

        Assert.Equal(expected.GetPixel(4, 7), actual.GetPixel(4, 7));
    }

    [Fact]
    public void ToIcon_MakesAnIconOfTheImageSize()
    {
        using var image = ControllerGlyph.Render(24, ControllerGlyphColors.DesktopModeOn);

        using var icon = IconFile.ToIcon(image);

        Assert.Equal(new Size(24, 24), icon.Size);
    }

    [Fact]
    public void Write_StoresTheLargestImageAsPng()
    {
        using var stream = WriteGlyphs();
        var file = stream.ToArray();

        var largest = Entries(file).Single(entry => entry.Size == 256);

        Assert.Equal(PngSignature, file.AsSpan(largest.Offset, PngSignature.Length).ToArray());
    }

    [Fact]
    public void AppIcon_FileHoldsEveryIconSize()
    {
        using var resource = typeof(AppIcon).Assembly.GetManifestResourceStream("ControllerPC.ico")!;
        using var copy = new MemoryStream();
        resource.CopyTo(copy);

        var sizes = Entries(copy.ToArray()).Select(entry => entry.Size);

        Assert.Equal([16, 20, 24, 32, 40, 48, 64, 256], sizes);
    }

    /// <summary>The (size, data offset) of each ICONDIRENTRY; a stored width of 0 means 256.</summary>
    private static IEnumerable<(int Size, int Offset)> Entries(byte[] file)
    {
        const int CountOffset = 4;
        const int FirstEntryOffset = 6;
        const int EntrySize = 16;
        const int DataOffsetInEntry = 12;

        var count = BitConverter.ToInt16(file, CountOffset);
        for (var i = 0; i < count; i++)
        {
            var entry = FirstEntryOffset + (i * EntrySize);
            var width = file[entry];
            yield return (width == 0 ? 256 : width, BitConverter.ToInt32(file, entry + DataOffsetInEntry));
        }
    }

    private static MemoryStream WriteGlyphs()
    {
        var images = Sizes.Select(size => ControllerGlyph.Render(size, ControllerGlyphColors.Neutral)).ToList();
        var stream = new MemoryStream();
        IconFile.Write(stream, images);
        images.ForEach(image => image.Dispose());
        stream.Position = 0;
        return stream;
    }
}
