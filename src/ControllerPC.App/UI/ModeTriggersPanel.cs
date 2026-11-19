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

    /// <param name="settings">The settings whose Base Triggers are edited in place.</param>
    /// <param name="controllerSource">Used to record the triggers.</param>
    public ModeTriggersPanel(AppSettings settings, IControllerSource controllerSource)
    {
        AutoScaleMode = AutoScaleMode.Inherit;

        var entryGroup = TriggerGroup("Base Entry Trigger", 0, settings.BaseEntryTrigger, controllerSource);
        var exitGroup = TriggerGroup("Base Exit Trigger", entryGroup.Right + GroupGap, settings.BaseExitTrigger, controllerSource);
        Controls.AddRange([entryGroup, exitGroup]);
        Size = new Size(exitGroup.Right, exitGroup.Bottom);
    }

    private static GroupBox TriggerGroup(string title, int x, ModeTrigger trigger, IControllerSource controllerSource)
    {
        var editor = new ModeTriggerEditor(controllerSource) { Location = new Point(GroupPadding, GroupTop) };
        editor.Edit(trigger);
        var group = ClassicControls.Group(title, x, 0, editor.Width + (2 * GroupPadding), editor.Height + GroupTop + GroupPadding);
        group.Controls.Add(editor);
        return group;
    }
}
