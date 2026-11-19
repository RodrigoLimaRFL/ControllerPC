using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ControllerPC.App.UI;

/// <summary>
/// Draws the app's gamepad glyph: a rounded body with two grips, a D-pad on the left and four face buttons on the
/// right. The shapes sit on a 16 x 16 grid so that at 16 pixels every detail is whole pixels, and scale up from there.
/// </summary>
internal static class ControllerGlyph
{
    private const float GridSize = 16;

    // Below this size, face buttons are drawn as single grid squares: round dots of one or two pixels blur away.
    private const int RoundButtonsMinimumSize = 24;

    // Body silhouette: a wide rounded bar with a grip hanging below each end. The edge is the silhouette filled with
    // the edge color; the body is the same shapes shrunk by the edge width, so only the outer outline shows.
    private static readonly RectangleF Bar = new(0, 3, 16, 8);
    private const float BarRadius = 3.5f;
    private static readonly RectangleF LeftGrip = new(0, 5, 7, 9);
    private static readonly RectangleF RightGrip = new(9, 5, 7, 9);
    private const float GripRadius = 3.5f;

    // One grid square is one pixel at 16 x 16 but a heavy halo at large sizes, so from ThinEdgeMinimumSize the edge
    // is half a square.
    private const float EdgeWidth = 1;
    private const float ThinEdgeWidth = 0.5f;
    private const int ThinEdgeMinimumSize = 32;

    // D-pad: a plus centred on (4.5, 7.5), one square thick.
    private static readonly RectangleF DPadVertical = new(4, 5, 1, 5);
    private static readonly RectangleF DPadHorizontal = new(2, 7, 5, 1);

    // Face buttons: a diamond of four squares around (11.5, 7.5).
    private static readonly PointF[] FaceButtons = [new(11, 5), new(9, 7), new(13, 7), new(11, 9)];
    private const float FaceButtonSize = 1;
    private const float RoundFaceButtonGrowth = 0.3f;

    /// <summary>A new <paramref name="size"/> x <paramref name="size"/> bitmap of the glyph; the caller disposes it.</summary>
    public static Bitmap Render(int size, ControllerGlyphColors colors)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        Draw(graphics, size, colors);
        return bitmap;
    }

    private static void Draw(Graphics graphics, int size, ControllerGlyphColors colors)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Integer grid coordinates fall on pixel edges, so whole grid squares fill whole pixels at 16 x 16.
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.ScaleTransform(size / GridSize, size / GridSize);

        using (var silhouette = Silhouette(inset: 0))
        using (var body = Silhouette(inset: size >= ThinEdgeMinimumSize ? ThinEdgeWidth : EdgeWidth))
        using (var edgeBrush = new SolidBrush(colors.Edge))
        using (var bodyBrush = new SolidBrush(colors.Body))
        {
            graphics.FillPath(edgeBrush, silhouette);
            graphics.FillPath(bodyBrush, body);
        }

        using var detailBrush = new SolidBrush(colors.Details);
        graphics.FillRectangle(detailBrush, DPadVertical);
        graphics.FillRectangle(detailBrush, DPadHorizontal);
        foreach (var button in FaceButtons)
        {
            if (size >= RoundButtonsMinimumSize)
            {
                var grown = FaceButtonSize + (2 * RoundFaceButtonGrowth);
                graphics.FillEllipse(
                    detailBrush, button.X - RoundFaceButtonGrowth, button.Y - RoundFaceButtonGrowth, grown, grown);
            }
            else
            {
                graphics.FillRectangle(detailBrush, button.X, button.Y, FaceButtonSize, FaceButtonSize);
            }
        }
    }

    /// <summary>The body shapes shrunk by <paramref name="inset"/> on every side, as one path.</summary>
    private static GraphicsPath Silhouette(float inset)
    {
        // Winding fill turns the overlapping shapes into one silhouette.
        var path = new GraphicsPath(FillMode.Winding);
        AddRoundedRectangle(path, Shrink(Bar, inset), BarRadius - inset);
        AddRoundedRectangle(path, Shrink(LeftGrip, inset), GripRadius - inset);
        AddRoundedRectangle(path, Shrink(RightGrip, inset), GripRadius - inset);
        return path;
    }

    private static RectangleF Shrink(RectangleF bounds, float inset) =>
        RectangleF.Inflate(bounds, -inset, -inset);

    private static void AddRoundedRectangle(GraphicsPath path, RectangleF bounds, float radius)
    {
        var diameter = 2 * radius;
        path.StartFigure();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
    }
}
