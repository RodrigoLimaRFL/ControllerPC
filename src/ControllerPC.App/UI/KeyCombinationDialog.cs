using ControllerPC.Core.Output;

namespace ControllerPC.App.UI;

/// <summary>Captures one Key Combination, e.g. for a Macro. OK stays disabled until a key was captured.</summary>
internal sealed class KeyCombinationDialog : ClassicDialog
{
    private const int FieldX = DialogMargin + 50;

    private readonly KeyCombinationEditor _editor = new() { Location = new Point(FieldX, DialogMargin) };
    private readonly Button _okButton;

    public KeyCombinationDialog(string title)
        : base(title)
    {
        var clientWidth = _editor.Right + DialogMargin;
        var buttonsY = _editor.Bottom + DialogMargin;
        _okButton = ClassicControls.Button("OK", ButtonRowX(clientWidth, 1), buttonsY);
        _okButton.DialogResult = DialogResult.OK;
        var cancelButton = ClassicControls.Button("Cancel", ButtonRowX(clientWidth, 0), buttonsY);
        cancelButton.DialogResult = DialogResult.Cancel;

        // No AcceptButton or CancelButton: Enter and Esc are keys the user may want to capture.
        Controls.AddRange([ClassicControls.Label("Keys:", DialogMargin, DialogMargin), _editor, _okButton, cancelButton]);
        _editor.ValueChanged += (_, _) => _okButton.Enabled = !_editor.Value.IsEmpty;
        _okButton.Enabled = false;
        CompleteLayout(new Size(clientWidth, buttonsY + ClassicControls.ButtonHeight + DialogMargin));
    }

    /// <summary>The captured Key Combination; read it after the dialog returns <see cref="DialogResult.OK"/>.</summary>
    public KeyCombination Result => _editor.Value;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _editor.FocusCapture();
    }
}
