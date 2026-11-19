namespace ControllerPC.App.UI;

/// <summary>Asks for a Profile name.</summary>
internal sealed class ProfileNameDialog : ClassicDialog
{
    private const int ClientWidth = 320;
    private const int ButtonsY = 70;

    private readonly TextBox _nameBox = new()
    {
        Location = new Point(DialogMargin, 34),
        Width = ClientWidth - (2 * DialogMargin),
    };

    /// <param name="title">Window title, e.g. "New Profile".</param>
    /// <param name="initialName">Name shown selected when the dialog opens.</param>
    public ProfileNameDialog(string title, string initialName)
        : base(title)
    {
        _nameBox.Text = initialName;
        var okButton = ClassicControls.Button("OK", ButtonRowX(ClientWidth, 1), ButtonsY);
        okButton.DialogResult = DialogResult.OK;
        var cancelButton = ClassicControls.Button("Cancel", ButtonRowX(ClientWidth, 0), ButtonsY);
        cancelButton.DialogResult = DialogResult.Cancel;
        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.AddRange([ClassicControls.Label("Profile name:", DialogMargin, 10), _nameBox, okButton, cancelButton]);
        CompleteLayout(new Size(ClientWidth, ButtonsY + ClassicControls.ButtonHeight + DialogMargin));
    }

    /// <summary>The name entered, without surrounding spaces.</summary>
    public string ProfileName => _nameBox.Text.Trim();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _nameBox.SelectAll();
        _nameBox.Focus();
    }
}
