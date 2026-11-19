using ControllerPC.Core.Controller;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>Edits the Base Entry Trigger and the Base Exit Trigger side by side.</summary>
internal sealed class ModeTriggersPanel : UserControl
{
    private const int GroupPadding = 10;
    private const int GroupTop = 22;
    private const int GroupGap = 10;

    private readonly ModeTriggerEditor _entryEditor;
    private readonly ModeTriggerEditor _exitEditor;

    /// <param name="settings">The settings whose Base Triggers are edited in place.</param>
    /// <param name="controllerSource">Used to record the triggers.</param>
    public ModeTriggersPanel(AppSettings settings, IControllerSource controllerSource)
    {
        AutoScaleMode = AutoScaleMode.Inherit;

        _entryEditor = new ModeTriggerEditor(controllerSource);
        _exitEditor = new ModeTriggerEditor(controllerSource);
        var entryGroup = TriggerGroup("Base Entry Trigger", 0, _entryEditor, settings.BaseEntryTrigger);
        var exitGroup = TriggerGroup("Base Exit Trigger", entryGroup.Right + GroupGap, _exitEditor, settings.BaseExitTrigger);
        Controls.AddRange([entryGroup, exitGroup]);
        Size = new Size(exitGroup.Right, exitGroup.Bottom);
    }

    /// <summary>Raised just before either Base Trigger starts recording from the controller.</summary>
    public event EventHandler? RecordingStarted
    {
        add
        {
            _entryEditor.RecordingStarted += value;
            _exitEditor.RecordingStarted += value;
        }

        remove
        {
            _entryEditor.RecordingStarted -= value;
            _exitEditor.RecordingStarted -= value;
        }
    }

    /// <summary>Raised when a recording started from this panel ends.</summary>
    public event EventHandler? RecordingFinished
    {
        add
        {
            _entryEditor.RecordingFinished += value;
            _exitEditor.RecordingFinished += value;
        }

        remove
        {
            _entryEditor.RecordingFinished -= value;
            _exitEditor.RecordingFinished -= value;
        }
    }

    private static GroupBox TriggerGroup(string title, int x, ModeTriggerEditor editor, ModeTrigger trigger)
    {
        editor.Location = new Point(GroupPadding, GroupTop);
        editor.Edit(trigger);
        var group = ClassicControls.Group(title, x, 0, editor.Width + (2 * GroupPadding), editor.Height + GroupTop + GroupPadding);
        group.Controls.Add(editor);
        return group;
    }
}
