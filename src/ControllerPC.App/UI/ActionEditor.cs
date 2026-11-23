using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>
/// Chooses one Action: its kind, then the mouse button, Key Combination, Nudge direction, Profile or Macro.
/// </summary>
internal sealed class ActionEditor : UserControl
{
    private const int FieldX = 80;
    private const int FieldWidth = KeyCombinationEditor.EditorWidth;

    private readonly ComboBox _kindBox = ClassicControls.DropDown(FieldX, 0, FieldWidth);
    private readonly Label _detailLabel = ClassicControls.Label(string.Empty, 0, ClassicControls.RowHeight);
    private readonly ComboBox _mouseButtonBox = ClassicControls.DropDown(FieldX, ClassicControls.RowHeight, FieldWidth);
    private readonly ComboBox _nudgeDirectionBox = ClassicControls.DropDown(FieldX, ClassicControls.RowHeight, FieldWidth);
    private readonly ComboBox _profileBox = ClassicControls.DropDown(FieldX, ClassicControls.RowHeight, FieldWidth);
    private readonly KeyCombinationEditor _keysEditor = new() { Location = new Point(FieldX, ClassicControls.RowHeight) };

    // A Macro needs the whole width for its list and buttons.
    private readonly MacroEditor _macroEditor = new() { Location = new Point(0, ClassicControls.RowHeight) };

    private bool _loading;

    public ActionEditor()
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        Size = new Size(
            Math.Max(FieldX + FieldWidth, _macroEditor.Right),
            Math.Max(_keysEditor.Bottom, _macroEditor.Bottom));

        AddItems(_kindBox, Enum.GetValues<ActionKind>(), DisplayNames.ForActionKind);
        AddItems(_mouseButtonBox, Enum.GetValues<MouseButton>(), DisplayNames.ForMouseButton);
        AddItems(_nudgeDirectionBox, Enum.GetValues<NudgeDirection>(), DisplayNames.ForNudgeDirection);

        Controls.AddRange(
        [
            ClassicControls.Label("Action:", 0, 0), _kindBox, _detailLabel,
            _mouseButtonBox, _nudgeDirectionBox, _profileBox, _keysEditor, _macroEditor,
        ]);

        _kindBox.SelectedIndexChanged += (_, _) => OnEdited();
        _mouseButtonBox.SelectedIndexChanged += (_, _) => OnEdited();
        _nudgeDirectionBox.SelectedIndexChanged += (_, _) => OnEdited();
        _profileBox.SelectedIndexChanged += (_, _) => OnEdited();
        _keysEditor.ValueChanged += (_, _) => OnEdited();
        _macroEditor.MacroChanged += (_, _) => OnEdited();

        Display(NoAction.Instance, []);
    }

    /// <summary>Raised when the user changes the Action.</summary>
    public event EventHandler? ActionChanged;

    public ButtonAction Action { get; private set; } = NoAction.Instance;

    /// <summary>Shows <paramref name="action"/>; <paramref name="profiles"/> are the "Switch to Profile" targets.</summary>
    public void Display(ButtonAction action, IReadOnlyList<Profile> profiles)
    {
        _loading = true;
        Action = action;
        LoadProfiles(profiles, (action as SwitchToProfileAction)?.ProfileId);
        ClassicControls.Select(_kindBox, KindOf(action));
        ClassicControls.Select(_mouseButtonBox, (action as MouseButtonAction)?.Button ?? MouseButton.Left);
        ClassicControls.Select(_nudgeDirectionBox, (action as NudgeAction)?.Direction ?? NudgeDirection.Up);
        _keysEditor.SetValue((action as KeyCombinationAction)?.Keys ?? new KeyCombination([]));
        _macroEditor.Display(action as MacroAction);
        ShowDetailFor(KindOf(action));
        _loading = false;
    }

    private static ActionKind KindOf(ButtonAction action) => action switch
    {
        MouseButtonAction => ActionKind.MouseButton,
        KeyCombinationAction => ActionKind.KeyCombination,
        NudgeAction => ActionKind.Nudge,
        NextProfileAction => ActionKind.NextProfile,
        SwitchToProfileAction => ActionKind.SwitchToProfile,
        MacroAction => ActionKind.Macro,
        _ => ActionKind.None,
    };

    private static void AddItems<T>(ComboBox box, IEnumerable<T> values, Func<T, string> text) =>
        box.Items.AddRange([.. values.Select(value => new ListItem<T>(value, text(value)))]);

    private void LoadProfiles(IReadOnlyList<Profile> profiles, Guid? targetId)
    {
        _profileBox.Items.Clear();
        AddItems(_profileBox, profiles.Select(p => p.Id), id => profiles.First(p => p.Id == id).Name);

        // Keep a dangling target visible instead of silently retargeting it; the validator warns about it.
        if (targetId is { } id && profiles.All(p => p.Id != id))
        {
            _profileBox.Items.Add(new ListItem<Guid>(id, "(missing Profile)"));
        }

        if (targetId is { } selected)
        {
            ClassicControls.Select(_profileBox, selected);
        }
        else if (_profileBox.Items.Count > 0)
        {
            _profileBox.SelectedIndex = 0;
        }
    }

    private void OnEdited()
    {
        if (_loading)
        {
            return;
        }

        var kind = ClassicControls.SelectedValue(_kindBox, ActionKind.None);
        ShowDetailFor(kind);
        Action = BuildAction(kind);
        ActionChanged?.Invoke(this, EventArgs.Empty);
    }

    private ButtonAction BuildAction(ActionKind kind) => kind switch
    {
        ActionKind.MouseButton => new MouseButtonAction(ClassicControls.SelectedValue(_mouseButtonBox, MouseButton.Left)),
        ActionKind.KeyCombination => new KeyCombinationAction(_keysEditor.Value),
        ActionKind.Nudge => new NudgeAction(ClassicControls.SelectedValue(_nudgeDirectionBox, NudgeDirection.Up)),
        ActionKind.NextProfile => new NextProfileAction(),
        ActionKind.SwitchToProfile when _profileBox.SelectedItem is ListItem<Guid> target =>
            new SwitchToProfileAction(target.Value),
        ActionKind.Macro => _macroEditor.Macro,
        _ => NoAction.Instance,
    };

    private void ShowDetailFor(ActionKind kind)
    {
        _mouseButtonBox.Visible = kind == ActionKind.MouseButton;
        _keysEditor.Visible = kind == ActionKind.KeyCombination;
        _macroEditor.Visible = kind == ActionKind.Macro;
        _nudgeDirectionBox.Visible = kind == ActionKind.Nudge;
        _profileBox.Visible = kind == ActionKind.SwitchToProfile;
        _detailLabel.Text = kind switch
        {
            ActionKind.MouseButton => "Button:",
            ActionKind.KeyCombination => "Keys:",
            ActionKind.Nudge => "Direction:",
            ActionKind.SwitchToProfile => "Profile:",
            _ => string.Empty,
        };
    }
}
