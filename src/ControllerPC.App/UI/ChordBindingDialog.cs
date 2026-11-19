using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>
/// Adds or edits one Chord Binding: its Chord (ticked by hand or recorded from the controller) and its Action.
/// OK stays disabled until the Chord has at least two buttons; every other rule is left to the settings validation.
/// </summary>
internal sealed class ChordBindingDialog : ClassicDialog
{
    private const int MinimumChordButtons = 2;
    private const int GroupPadding = 10;
    private const int GroupTop = 22;
    private const int GroupGap = 8;

    private readonly IControllerSource _controllerSource;
    private readonly ChordPicker _chordPicker = new() { Location = new Point(GroupPadding, GroupTop) };
    private readonly ActionEditor _actionEditor = new() { Location = new Point(GroupPadding, GroupTop) };
    private readonly Button _okButton;

    /// <param name="title">Window title, e.g. "Add Chord Binding".</param>
    /// <param name="binding">The Chord Binding to show first (not changed by the dialog).</param>
    /// <param name="profiles">The "Switch to Profile" targets.</param>
    /// <param name="controllerSource">Used to record the Chord.</param>
    public ChordBindingDialog(string title, ChordBinding binding, IReadOnlyList<Profile> profiles, IControllerSource controllerSource)
        : base(title)
    {
        _controllerSource = controllerSource;

        var recordButton = ClassicControls.Button("&Record...", GroupPadding, _chordPicker.Bottom + GroupGap, (_, _) => RecordChord());
        var hint = ClassicControls.Label(
            "Tick two or more buttons, or record them.", recordButton.Right + GroupGap, recordButton.Top);
        var chordGroup = ClassicControls.Group(
            "Chord",
            DialogMargin,
            DialogMargin,
            _chordPicker.Width + (2 * GroupPadding),
            recordButton.Bottom + GroupPadding);
        chordGroup.Controls.AddRange([_chordPicker, recordButton, hint]);

        var actionGroup = ClassicControls.Group(
            "Action",
            DialogMargin,
            chordGroup.Bottom + GroupGap,
            chordGroup.Width,
            _actionEditor.Height + GroupTop + GroupPadding);
        actionGroup.Controls.Add(_actionEditor);

        var clientWidth = chordGroup.Right + DialogMargin;
        var buttonsY = actionGroup.Bottom + DialogMargin;
        _okButton = ClassicControls.Button("OK", ButtonRowX(clientWidth, 1), buttonsY);
        _okButton.DialogResult = DialogResult.OK;
        var cancelButton = ClassicControls.Button("Cancel", ButtonRowX(clientWidth, 0), buttonsY);
        cancelButton.DialogResult = DialogResult.Cancel;
        AcceptButton = _okButton;
        CancelButton = cancelButton;

        Controls.AddRange([chordGroup, actionGroup, _okButton, cancelButton]);

        _chordPicker.SetChord(binding.Chord);
        _actionEditor.Display(binding.Action, profiles);
        _chordPicker.ChordChanged += (_, _) => UpdateOkButton();
        UpdateOkButton();
        CompleteLayout(new Size(clientWidth, buttonsY + ClassicControls.ButtonHeight + DialogMargin));
    }

    /// <summary>Raised just before the Chord starts recording from the controller.</summary>
    public event EventHandler? RecordingStarted;

    /// <summary>Raised when the recording ends, however it ended (always paired with <see cref="RecordingStarted"/>).</summary>
    public event EventHandler? RecordingFinished;

    /// <summary>The Chord Binding as edited; read it after the dialog returns <see cref="DialogResult.OK"/>.</summary>
    public ChordBinding Result => new() { Chord = _chordPicker.Chord, Action = _actionEditor.Action };

    private void RecordChord()
    {
        using var dialog = RecordChordsDialog.ForChord(_controllerSource);
        var chords = dialog.Record(
            this,
            () => RecordingStarted?.Invoke(this, EventArgs.Empty),
            () => RecordingFinished?.Invoke(this, EventArgs.Empty));
        if (chords.Count > 0)
        {
            _chordPicker.SetChord(chords[0]);
            UpdateOkButton();
        }
    }

    private void UpdateOkButton() =>
        _okButton.Enabled = ControllerButtons.Split(_chordPicker.Chord.Buttons).Count() >= MinimumChordButtons;
}
