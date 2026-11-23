using ControllerPC.Core.Output;

namespace ControllerPC.App.UI;

/// <summary>
/// Edits one Key Combination: a <see cref="KeyCombinationCaptureBox"/>, a "Win" check box for the combinations
/// Windows keeps for itself, and a hint saying so.
/// </summary>
internal sealed class KeyCombinationEditor : UserControl
{
    public const int EditorWidth = 230;
    private const int HintHeight = 50;
    private const int HintOffset = -4;

    private readonly KeyCombinationCaptureBox _keysBox = new() { Location = Point.Empty, Width = EditorWidth };
    private readonly CheckBox _winBox = ClassicControls.CheckBox("Win", 0, ClassicControls.RowHeight);
    private readonly Label _winHint = new()
    {
        Text = "Windows keeps many Win combinations for itself; tick Win to add the Win key.",
        Location = new Point(0, (2 * ClassicControls.RowHeight) + HintOffset),
        Size = new Size(EditorWidth, HintHeight),
    };

    private bool _loading;

    public KeyCombinationEditor()
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        Size = new Size(EditorWidth, _winHint.Bottom);
        Controls.AddRange([_keysBox, _winBox, _winHint]);

        _keysBox.ValueChanged += (_, _) =>
        {
            ShowWin();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        };
        _winBox.CheckedChanged += (_, _) =>
        {
            if (!_loading)
            {
                _keysBox.SetWin(_winBox.Checked);
            }
        };
    }

    /// <summary>Raised when the user captures a new Key Combination or toggles Win.</summary>
    public event EventHandler? ValueChanged;

    public KeyCombination Value => _keysBox.Value;

    /// <summary>Shows <paramref name="keys"/> without raising <see cref="ValueChanged"/>.</summary>
    public void SetValue(KeyCombination keys)
    {
        _keysBox.SetValue(keys);
        ShowWin();
    }

    /// <summary>Puts the keyboard focus in the capture box, ready for keys.</summary>
    public void FocusCapture()
    {
        _keysBox.Focus();

        // Focus selects the prompt text, which looks like editable text; the box only captures keys.
        _keysBox.Select(0, 0);
    }

    private void ShowWin()
    {
        _loading = true;
        _winBox.Checked = KeyCombinationComposer.HasWin(_keysBox.Value);
        _loading = false;
    }
}
