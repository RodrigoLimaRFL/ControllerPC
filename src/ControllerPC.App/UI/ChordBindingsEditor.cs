using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>Lists a Profile's Chord Bindings (Add / Edit / Remove) and edits its Chord Wait.</summary>
internal sealed class ChordBindingsEditor : UserControl
{
    private const int ListWidth = 310;
    private const int ListHeight = 82;
    private const int ButtonGap = 6;
    private const int RowGap = 6;
    private const int WaitFieldX = 120;
    private const int WaitFieldWidth = 70;
    private const decimal WaitIncrementMilliseconds = 5;

    private readonly IControllerSource _controllerSource;
    private readonly ListBox _list = new()
    {
        Location = Point.Empty,
        Size = new Size(ListWidth, ListHeight),
        IntegralHeight = false,
        HorizontalScrollbar = true,
    };

    private readonly Button _editButton;
    private readonly Button _removeButton;
    private readonly NumericUpDown _waitBox;

    private Profile _profile = new();
    private IReadOnlyList<Profile> _allProfiles = [];
    private bool _loading;

    /// <param name="controllerSource">Used to record Chords.</param>
    public ChordBindingsEditor(IControllerSource controllerSource)
    {
        _controllerSource = controllerSource;
        AutoScaleMode = AutoScaleMode.Inherit;

        var buttonsY = _list.Bottom + RowGap;
        var addButton = ClassicControls.Button("Add...", 0, buttonsY, (_, _) => AddBinding());
        _editButton = ClassicControls.Button("Edit...", addButton.Right + ButtonGap, buttonsY, (_, _) => EditSelected());
        _removeButton = ClassicControls.Button("Remove", _editButton.Right + ButtonGap, buttonsY, (_, _) => RemoveSelected());

        var waitY = addButton.Bottom + RowGap;
        _waitBox = ClassicControls.Number(
            WaitFieldX, waitY, WaitFieldWidth, 0, (decimal)Profile.MaxChordWait.TotalMilliseconds, WaitIncrementMilliseconds);

        Controls.AddRange(
        [
            _list, addButton, _editButton, _removeButton,
            ClassicControls.Label("Chord Wait:", 0, waitY), _waitBox,
            ClassicControls.Label("ms", WaitFieldX + WaitFieldWidth + ButtonGap, waitY),
        ]);
        Size = new Size(ListWidth, _waitBox.Bottom);

        _list.SelectedIndexChanged += (_, _) => UpdateButtons();
        _list.DoubleClick += (_, _) => EditSelected();
        _waitBox.ValueChanged += (_, _) => StoreChordWait();
    }

    /// <summary>Raised just before a Chord starts recording from the controller.</summary>
    public event EventHandler? RecordingStarted;

    /// <summary>Raised when a recording started from this editor ends.</summary>
    public event EventHandler? RecordingFinished;

    /// <summary>
    /// Edits the Chord Bindings and Chord Wait of <paramref name="profile"/> in place; <paramref name="allProfiles"/>
    /// are the "Switch to Profile" targets.
    /// </summary>
    public void Edit(Profile profile, IReadOnlyList<Profile> allProfiles)
    {
        _profile = profile;
        _allProfiles = allProfiles;
        _loading = true;
        ClassicControls.SetClamped(_waitBox, (decimal)profile.ChordWait.TotalMilliseconds);
        _loading = false;
        ShowBindings(selectedIndex: 0);
    }

    private void ShowBindings(int selectedIndex)
    {
        _list.Items.Clear();
        foreach (var binding in _profile.ChordBindings)
        {
            _list.Items.Add(DisplayNames.ForChordBinding(binding, _allProfiles));
        }

        _list.SelectedIndex = _list.Items.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, _list.Items.Count - 1);
        UpdateButtons();
    }

    private void UpdateButtons() => _editButton.Enabled = _removeButton.Enabled = _list.SelectedIndex >= 0;

    private void AddBinding()
    {
        if (AskBinding("Add Chord Binding", new ChordBinding()) is { } added)
        {
            _profile.ChordBindings.Add(added);
            ShowBindings(selectedIndex: _profile.ChordBindings.Count - 1);
        }
    }

    private void EditSelected()
    {
        var index = _list.SelectedIndex;
        if (index >= 0 && AskBinding("Edit Chord Binding", _profile.ChordBindings[index]) is { } edited)
        {
            _profile.ChordBindings[index] = edited;
            ShowBindings(index);
        }
    }

    private void RemoveSelected()
    {
        var index = _list.SelectedIndex;
        if (index >= 0)
        {
            _profile.ChordBindings.RemoveAt(index);
            ShowBindings(index);
        }
    }

    /// <summary>Shows the Chord Binding dialog; the edited binding, or <c>null</c> when cancelled.</summary>
    private ChordBinding? AskBinding(string title, ChordBinding binding)
    {
        using var dialog = new ChordBindingDialog(title, binding, _allProfiles, _controllerSource);
        dialog.RecordingStarted += (_, _) => RecordingStarted?.Invoke(this, EventArgs.Empty);
        dialog.RecordingFinished += (_, _) => RecordingFinished?.Invoke(this, EventArgs.Empty);
        return dialog.ShowDialog(FindForm()) == DialogResult.OK ? dialog.Result : null;
    }

    private void StoreChordWait()
    {
        if (!_loading)
        {
            _profile.ChordWait = TimeSpan.FromMilliseconds((double)_waitBox.Value);
        }
    }
}
