namespace ControllerPC.App.UI;

/// <summary>
/// A fixed-size dialog window laid out in logical pixels at 96 DPI with the default font, scaled to the actual
/// font and DPI once the derived constructor calls <see cref="CompleteLayout"/>.
/// </summary>
public abstract class ClassicDialog : Form
{
    // The size of the default font (Segoe UI 9pt) at 96 DPI, which the logical layout is designed for.
    private static readonly SizeF DesignFontSize = new(7F, 15F);

    protected ClassicDialog(string title)
    {
        // Layout stays suspended until CompleteLayout, so auto-scaling runs once over the finished layout.
        SuspendLayout();
        AutoScaleDimensions = DesignFontSize;
        AutoScaleMode = AutoScaleMode.Font;
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
    }

    /// <summary>Gap between the window edge and its content.</summary>
    protected const int DialogMargin = 12;

    private const int ButtonGap = 6;

    /// <summary>
    /// X of a button in the right-aligned row of dialog buttons; <paramref name="positionFromRight"/> 0 is the
    /// rightmost one.
    /// </summary>
    protected static int ButtonRowX(int clientWidth, int positionFromRight) =>
        clientWidth - DialogMargin - ((positionFromRight + 1) * ClassicControls.ButtonWidth) - (positionFromRight * ButtonGap);

    /// <summary>Sets the logical client size and scales everything to the current font and DPI.</summary>
    protected void CompleteLayout(Size clientSize)
    {
        ClientSize = clientSize;
        ResumeLayout(false);
        PerformLayout();
    }
}
