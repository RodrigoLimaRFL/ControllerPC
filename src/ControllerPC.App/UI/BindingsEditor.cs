using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>
/// Lists every controller button with its Binding and edits the Action of the selected one; below that, the
/// Profile's Chord Bindings.
/// </summary>
internal sealed class BindingsEditor : UserControl
{
    private const int ListWidth = 280;
    private const int ListHeight = 360;
    private const int ButtonColumnWidth = 80;
    private const int GroupX = ListWidth + 8;
    private const int GroupPadding = 10;
    private const int GroupTop = 22;
    private const int GroupGap = 8;

    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false,
        MultiSelect = false,
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
        Location = Point.Empty,
        Size = new Size(ListWidth, ListHeight),
    };

    private readonly ActionEditor _actionEditor = new() { Location = new Point(GroupPadding, GroupTop) };
    private readonly GroupBox _actionGroup;
    private readonly ChordBindingsEditor _chordBindingsEditor;

    private Profile _profile = new();
    private IReadOnlyList<Profile> _allProfiles = [];

    private ControllerButton? SelectedButton =>
        _list.SelectedItems.Count == 1 ? (ControllerButton)_list.SelectedItems[0].Tag! : null;

    /// <param name="controllerSource">Used to record the Chords of Chord Bindings.</param>
    public BindingsEditor(IControllerSource controllerSource)
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        _actionGroup = ClassicControls.Group(
            "Action of the selected button",
            GroupX,
            0,
            _actionEditor.Width + (2 * GroupPadding),
            _actionEditor.Height + GroupTop + GroupPadding);
        _actionGroup.Controls.Add(_actionEditor);

        _chordBindingsEditor = new ChordBindingsEditor(controllerSource) { Location = new Point(GroupPadding, GroupTop) };
        var chordBindingsGroup = ClassicControls.Group(
            "Chord Bindings",
            GroupX,
            _actionGroup.Bottom + GroupGap,
            _actionGroup.Width,
            _chordBindingsEditor.Height + GroupTop + GroupPadding);
        chordBindingsGroup.Controls.Add(_chordBindingsEditor);
        Size = new Size(_actionGroup.Right, Math.Max(ListHeight, chordBindingsGroup.Bottom));

        _list.Columns.Add("Button");
        _list.Columns.Add("Action");
        foreach (var button in ControllerButtons.All)
        {
            _list.Items.Add(new ListViewItem([DisplayNames.ForButton(button), string.Empty]) { Tag = button });
        }

        Controls.AddRange([_list, _actionGroup, chordBindingsGroup]);
        SizeColumns();

        _list.ClientSizeChanged += (_, _) => SizeColumns();
        _list.SelectedIndexChanged += (_, _) => ShowSelectedAction();
        _actionEditor.ActionChanged += (_, _) => StoreSelectedAction();
        _list.Items[0].Selected = true;
    }

    /// <summary>Raised just before the Chord of a Chord Binding starts recording from the controller.</summary>
    public event EventHandler? RecordingStarted
    {
        add => _chordBindingsEditor.RecordingStarted += value;
        remove => _chordBindingsEditor.RecordingStarted -= value;
    }

    /// <summary>Raised when a recording started from this editor ends.</summary>
    public event EventHandler? RecordingFinished
    {
        add => _chordBindingsEditor.RecordingFinished += value;
        remove => _chordBindingsEditor.RecordingFinished -= value;
    }

    /// <summary>
    /// Edits the Bindings and Chord Bindings of <paramref name="profile"/> in place; <paramref name="allProfiles"/>
    /// are the "Switch to Profile" targets.
    /// </summary>
    public void Edit(Profile profile, IReadOnlyList<Profile> allProfiles)
    {
        _profile = profile;
        _allProfiles = allProfiles;
        _chordBindingsEditor.Edit(profile, allProfiles);
        foreach (ListViewItem item in _list.Items)
        {
            ShowAction(item);
        }

        ShowSelectedAction();
    }

    // Column widths are not scaled with the form, so they follow the list's current (scaled) width.
    private void SizeColumns()
    {
        var buttonColumn = LogicalToDeviceUnits(ButtonColumnWidth);
        _list.Columns[0].Width = buttonColumn;
        _list.Columns[1].Width = _list.ClientSize.Width - buttonColumn;
    }

    private void ShowAction(ListViewItem item) =>
        item.SubItems[1].Text = DisplayNames.ForAction(_profile.GetAction((ControllerButton)item.Tag!), _allProfiles);

    private void ShowSelectedAction()
    {
        _actionGroup.Enabled = SelectedButton is not null;
        if (SelectedButton is { } button)
        {
            _actionEditor.Display(_profile.GetAction(button), _allProfiles);
        }
    }

    private void StoreSelectedAction()
    {
        if (SelectedButton is not { } button)
        {
            return;
        }

        if (_actionEditor.Action is NoAction)
        {
            _profile.Bindings.Remove(button);
        }
        else
        {
            _profile.Bindings[button] = _actionEditor.Action;
        }

        ShowAction(_list.SelectedItems[0]);
    }
}
