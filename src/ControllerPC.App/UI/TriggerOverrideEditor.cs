using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>
/// Edits one optional Trigger Override of a Profile: "Use own trigger" on gives the Profile its own Mode Trigger,
/// off means the Base Trigger applies.
/// </summary>
internal sealed class TriggerOverrideEditor : UserControl
{
    private const int EditorTop = 50;
    private const int BaseTriggerIndent = 18;
    private const int BaseTriggerLabelHeight = 17;

    private readonly CheckBox _useOwnBox;
    private readonly Label _baseTriggerLabel = new() { Location = new Point(BaseTriggerIndent, 24), AutoEllipsis = true };
    private readonly ModeTriggerEditor _triggerEditor;
    private readonly string _baseTriggerName;

    private ModeTrigger? _override;

    // Kept while the box is unticked so that ticking it again restores what the user had.
    private ModeTrigger? _setAside;
    private bool _loading;

    /// <param name="triggerName">"Entry Trigger" or "Exit Trigger", as used in the UI text.</param>
    /// <param name="controllerSource">Used to record the trigger.</param>
    public TriggerOverrideEditor(string triggerName, IControllerSource controllerSource)
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        _baseTriggerName = $"Base {triggerName}";
        _useOwnBox = ClassicControls.CheckBox($"Use own {triggerName}", 0, 0);
        _triggerEditor = new ModeTriggerEditor(controllerSource) { Location = new Point(0, EditorTop) };
        _baseTriggerLabel.Size = new Size(_triggerEditor.Width - BaseTriggerIndent, BaseTriggerLabelHeight);
        Size = new Size(_triggerEditor.Width, _triggerEditor.Bottom);

        Controls.AddRange([_useOwnBox, _baseTriggerLabel, _triggerEditor]);
        _useOwnBox.CheckedChanged += (_, _) => ToggleOverride();
    }

    /// <summary>Raised when the user turns the Trigger Override on or off; read <see cref="Override"/>.</summary>
    public event EventHandler? OverrideChanged;

    /// <summary>The Trigger Override being edited, or <c>null</c> when the Base Trigger applies.</summary>
    public ModeTrigger? Override => _override;

    /// <summary>
    /// Shows <paramref name="trigger"/> (edited in place) and, for reference, the Steps of
    /// <paramref name="baseTrigger"/>.
    /// </summary>
    public void Edit(ModeTrigger? trigger, ModeTrigger baseTrigger)
    {
        _loading = true;
        _override = trigger;
        _setAside = null;
        _useOwnBox.Checked = trigger is not null;
        _baseTriggerLabel.Text = $"Otherwise the {_baseTriggerName} applies: {DisplayNames.ForSteps(baseTrigger.Steps)}";
        _triggerEditor.Edit(trigger);
        _loading = false;
    }

    private void ToggleOverride()
    {
        if (_loading)
        {
            return;
        }

        if (_useOwnBox.Checked)
        {
            // A new override starts without Steps: the user records it (validation reports it until then).
            _override = _setAside ?? new ModeTrigger();
        }
        else
        {
            _setAside = _override;
            _override = null;
        }

        _triggerEditor.Edit(_override);
        OverrideChanged?.Invoke(this, EventArgs.Empty);
    }
}
