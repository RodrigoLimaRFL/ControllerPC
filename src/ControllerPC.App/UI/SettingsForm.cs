using ControllerPC.Core.Controller;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.UI;

/// <summary>
/// The settings window. It edits a working copy of the settings in place; OK and Apply check it with
/// <see cref="SettingsValidator"/> and hand a snapshot to the caller through <see cref="SettingsApplied"/>.
/// Saving and applying are up to the caller. Cancel (or closing the window) discards changes made since the
/// last Apply.
/// </summary>
public sealed class SettingsForm : ClassicDialog
{
    private const string Title = "ControllerPC Settings";
    private const string Bullet = "• ";
    private const int EdgeMargin = 8;
    private const int ButtonGap = 6;

    // Tab headers and page borders around the largest panel.
    private const int TabChromeWidth = 2 * EdgeMargin + 8;
    private const int TabChromeHeight = 2 * EdgeMargin + 30;

    private readonly AppSettings _settings;
    private readonly TabControl _tabs = new() { Location = new Point(EdgeMargin, EdgeMargin) };
    private readonly ProfilesPanel _profilesPanel;

    /// <param name="workingCopy">
    /// A copy of the live settings (e.g. from <see cref="SettingsSerializer.Clone"/>) with at least one Profile;
    /// the form changes it as the user edits.
    /// </param>
    /// <param name="controllerSource">Used to record Mode Triggers from the controller.</param>
    public SettingsForm(AppSettings workingCopy, IControllerSource controllerSource)
        : base(Title)
    {
        _settings = workingCopy;

        // Opened from the tray, so it is a top-level window of its own.
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;

        var modeTriggersPanel = new ModeTriggersPanel(workingCopy, controllerSource) { Location = new Point(EdgeMargin, EdgeMargin) };
        _profilesPanel = new ProfilesPanel(workingCopy, controllerSource) { Location = new Point(EdgeMargin, EdgeMargin) };
        _tabs.TabPages.AddRange([Page("Mode Triggers", modeTriggersPanel), Page("Profiles", _profilesPanel)]);
        _tabs.Size = new Size(
            Math.Max(modeTriggersPanel.Width, _profilesPanel.Width) + TabChromeWidth,
            Math.Max(modeTriggersPanel.Height, _profilesPanel.Height) + TabChromeHeight);

        // Base Triggers shown on the Profiles page may have changed on the Mode Triggers page.
        _tabs.SelectedIndexChanged += (_, _) => _profilesPanel.RefreshSelected();

        var buttonsY = _tabs.Bottom + EdgeMargin;
        var applyX = _tabs.Right - ClassicControls.ButtonWidth;
        var cancelX = applyX - ClassicControls.ButtonWidth - ButtonGap;
        var okX = cancelX - ClassicControls.ButtonWidth - ButtonGap;
        var okButton = ClassicControls.Button("OK", okX, buttonsY, (_, _) => AcceptAndClose());
        var cancelButton = ClassicControls.Button("Cancel", cancelX, buttonsY, (_, _) => Close());
        var applyButton = ClassicControls.Button("&Apply", applyX, buttonsY, (_, _) => TryApply());
        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.AddRange([_tabs, okButton, cancelButton, applyButton]);
        CompleteLayout(new Size(_tabs.Right + EdgeMargin, buttonsY + ClassicControls.ButtonHeight + EdgeMargin));
    }

    /// <summary>
    /// Raised by OK and Apply once the settings passed validation (and the user accepted any warnings).
    /// The argument is a deep copy of the edited settings that the caller may keep, save and apply.
    /// </summary>
    public event EventHandler<AppSettings>? SettingsApplied;

    private static TabPage Page(string text, Control content)
    {
        var page = new TabPage(text) { UseVisualStyleBackColor = true };
        page.Controls.Add(content);
        return page;
    }

    private void AcceptAndClose()
    {
        if (TryApply())
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private bool TryApply()
    {
        var issues = SettingsValidator.Validate(_settings);
        var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                $"The settings cannot be saved:\n\n{IssueList(errors)}",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        var warnings = issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();
        if (warnings.Count > 0)
        {
            var answer = MessageBox.Show(
                this,
                $"Please check:\n\n{IssueList(warnings)}\n\nSave the settings anyway?",
                Title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return false;
            }
        }

        SettingsApplied?.Invoke(this, SettingsSerializer.Clone(_settings));
        return true;
    }

    private string IssueList(IEnumerable<ValidationIssue> issues) =>
        string.Join("\n", issues.Select(issue => Bullet + IssueText(issue)));

    private string IssueText(ValidationIssue issue) =>
        issue.ProfileId is { } id && _settings.FindProfile(id) is { } profile
            ? $"Profile \"{profile.Name}\": {issue.Message}"
            : issue.Message;
}
