using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.UI;

/// <summary>Edits one Profile in place: Bindings, movement, and its Entry and Exit Trigger Overrides.</summary>
internal sealed class ProfileEditor : UserControl
{
    private const int PagePadding = 8;

    // Tab header plus page border, so the pages fit the largest editor.
    private const int TabChromeWidth = 2 * PagePadding + 8;
    private const int TabChromeHeight = 2 * PagePadding + 30;

    private readonly TabControl _tabs = new() { Location = Point.Empty };
    private readonly BindingsEditor _bindingsEditor = new() { Location = new Point(PagePadding, PagePadding) };
    private readonly MovementEditor _movementEditor = new() { Location = new Point(PagePadding, PagePadding) };
    private readonly TriggerOverrideEditor _entryEditor;
    private readonly TriggerOverrideEditor _exitEditor;

    private Profile? _profile;

    public ProfileEditor(IControllerSource controllerSource)
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        _entryEditor = new TriggerOverrideEditor("Entry Trigger", controllerSource) { Location = new Point(PagePadding, PagePadding) };
        _exitEditor = new TriggerOverrideEditor("Exit Trigger", controllerSource) { Location = new Point(PagePadding, PagePadding) };

        Control[] editors = [_bindingsEditor, _movementEditor, _entryEditor, _exitEditor];
        _tabs.TabPages.AddRange(
        [
            Page("Bindings", _bindingsEditor),
            Page("Movement", _movementEditor),
            Page("Entry Trigger", _entryEditor),
            Page("Exit Trigger", _exitEditor),
        ]);
        _tabs.Size = new Size(editors.Max(e => e.Width) + TabChromeWidth, editors.Max(e => e.Height) + TabChromeHeight);
        Size = _tabs.Size;
        Controls.Add(_tabs);

        _entryEditor.OverrideChanged += (_, _) =>
        {
            if (_profile is not null)
            {
                _profile.EntryTriggerOverride = _entryEditor.Override;
            }
        };
        _exitEditor.OverrideChanged += (_, _) =>
        {
            if (_profile is not null)
            {
                _profile.ExitTriggerOverride = _exitEditor.Override;
            }
        };
    }

    /// <summary>
    /// Edits <paramref name="profile"/> in place. <paramref name="settings"/> supplies the Base Triggers and the
    /// "Switch to Profile" targets.
    /// </summary>
    public void Edit(Profile profile, AppSettings settings)
    {
        _profile = profile;
        _bindingsEditor.Edit(profile, settings.Profiles);
        _movementEditor.Edit(profile);
        _entryEditor.Edit(profile.EntryTriggerOverride, settings.BaseEntryTrigger);
        _exitEditor.Edit(profile.ExitTriggerOverride, settings.BaseExitTrigger);
    }

    private static TabPage Page(string text, Control content)
    {
        var page = new TabPage(text) { UseVisualStyleBackColor = true };
        page.Controls.Add(content);
        return page;
    }
}
