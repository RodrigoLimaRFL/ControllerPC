using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;

namespace ControllerPC.App.UI;

/// <summary>The list of Profiles (New / Duplicate / Rename / Delete) and the editor of the selected one.</summary>
internal sealed class ProfilesPanel : UserControl
{
    private const int ListWidth = 2 * ClassicControls.ButtonWidth + 6;
    private const int ButtonGap = 6;
    private const string ActiveMarker = " (active)";

    private readonly AppSettings _settings;
    private readonly ListBox _profileList = new() { Location = Point.Empty, IntegralHeight = false };
    private readonly ProfileEditor _profileEditor;
    private readonly Button _deleteButton;

    /// <param name="settings">The settings being edited; Profiles are added, changed and removed in place.</param>
    /// <param name="controllerSource">Used to record Trigger Overrides.</param>
    public ProfilesPanel(AppSettings settings, IControllerSource controllerSource)
    {
        _settings = settings;
        AutoScaleMode = AutoScaleMode.Inherit;

        _profileEditor = new ProfileEditor(controllerSource) { Location = new Point(ListWidth + 10, 0) };
        Size = new Size(_profileEditor.Right, _profileEditor.Height);

        var secondRowY = Height - ClassicControls.ButtonHeight;
        var firstRowY = secondRowY - ClassicControls.ButtonHeight - ButtonGap;
        var secondColumnX = ClassicControls.ButtonWidth + ButtonGap;
        _profileList.Size = new Size(ListWidth, firstRowY - ButtonGap);
        _deleteButton = ClassicControls.Button("&Delete", secondColumnX, secondRowY, (_, _) => DeleteSelected());

        Controls.AddRange(
        [
            _profileList,
            ClassicControls.Button("&New...", 0, firstRowY, (_, _) => AddNew()),
            ClassicControls.Button("D&uplicate...", secondColumnX, firstRowY, (_, _) => DuplicateSelected()),
            ClassicControls.Button("Re&name...", 0, secondRowY, (_, _) => RenameSelected()),
            _deleteButton,
            _profileEditor,
        ]);

        _profileList.SelectedIndexChanged += (_, _) => EditSelected();
        ShowProfiles(settings.ActiveProfile);
    }

    /// <summary>Raised just before a Trigger Override of the selected Profile starts recording from the controller.</summary>
    public event EventHandler? RecordingStarted
    {
        add => _profileEditor.RecordingStarted += value;
        remove => _profileEditor.RecordingStarted -= value;
    }

    /// <summary>Raised when a recording started from this panel ends.</summary>
    public event EventHandler? RecordingFinished
    {
        add => _profileEditor.RecordingFinished += value;
        remove => _profileEditor.RecordingFinished -= value;
    }

    /// <summary>Shows the selected Profile again, e.g. after the Base Triggers changed on another page.</summary>
    public void RefreshSelected() => EditSelected();

    private Profile? SelectedProfile => (_profileList.SelectedItem as ListItem<Profile>)?.Value;

    /// <summary>
    /// A deep copy of <paramref name="profile"/> with a fresh id. The Entry Trigger Override is not copied: two
    /// identical Entry Triggers could never be told apart, so the copy uses the Base Entry Trigger instead.
    /// </summary>
    private static Profile Copy(Profile profile)
    {
        var holder = new AppSettings { Profiles = [profile], ActiveProfileId = profile.Id };
        var copy = SettingsSerializer.Clone(holder).Profiles[0];
        copy.Id = Guid.NewGuid();
        copy.EntryTriggerOverride = null;
        return copy;
    }

    private void ShowProfiles(Profile selected)
    {
        _profileList.Items.Clear();
        foreach (var profile in _settings.Profiles)
        {
            var marker = profile.Id == _settings.ActiveProfile.Id ? ActiveMarker : string.Empty;
            _profileList.Items.Add(new ListItem<Profile>(profile, profile.Name + marker));
        }

        _profileList.SelectedIndex = _settings.Profiles.IndexOf(selected);
        _deleteButton.Enabled = _settings.Profiles.Count > 1;
    }

    private void EditSelected()
    {
        if (SelectedProfile is { } profile)
        {
            _profileEditor.Edit(profile, _settings);
        }
    }

    private string? AskName(string title, string initialName)
    {
        using var dialog = new ProfileNameDialog(title, initialName);
        return dialog.ShowDialog(FindForm()) == DialogResult.OK && dialog.ProfileName.Length > 0 ? dialog.ProfileName : null;
    }

    private void AddNew()
    {
        if (AskName("New Profile", "New Profile") is { } name)
        {
            var profile = new Profile { Name = name };
            _settings.Profiles.Add(profile);
            ShowProfiles(profile);
        }
    }

    private void DuplicateSelected()
    {
        if (SelectedProfile is { } source && AskName("Duplicate Profile", $"{source.Name} (copy)") is { } name)
        {
            var copy = Copy(source);
            copy.Name = name;
            _settings.Profiles.Insert(_settings.Profiles.IndexOf(source) + 1, copy);
            ShowProfiles(copy);
        }
    }

    private void RenameSelected()
    {
        if (SelectedProfile is { } profile && AskName("Rename Profile", profile.Name) is { } name)
        {
            profile.Name = name;
            ShowProfiles(profile);
        }
    }

    private void DeleteSelected()
    {
        if (SelectedProfile is not { } profile || _settings.Profiles.Count <= 1)
        {
            return;
        }

        var answer = MessageBox.Show(
            FindForm(),
            $"Delete the Profile \"{profile.Name}\"?",
            "Delete Profile",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        var index = _settings.Profiles.IndexOf(profile);
        var wasActive = profile.Id == _settings.ActiveProfile.Id;
        _settings.Profiles.Remove(profile);
        if (wasActive)
        {
            _settings.ActiveProfileId = _settings.Profiles[0].Id;
        }

        ShowProfiles(_settings.Profiles[Math.Min(index, _settings.Profiles.Count - 1)]);
    }
}
