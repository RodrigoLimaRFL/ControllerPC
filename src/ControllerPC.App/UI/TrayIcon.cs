using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>
/// The notification-area icon and its menu. It only shows state and reports what the user picked; the caller
/// decides what happens.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const string AppName = "ControllerPC";
    private const string TitleSeparator = " — ";
    private const string Ellipsis = "…";

    /// <summary><see cref="NotifyIcon.Text"/> rejects longer text.</summary>
    private const int MaxToolTipLength = 127;

    /// <summary>Ignored by current Windows versions, which use the user's accessibility setting instead.</summary>
    private const int NoticeTimeoutMilliseconds = 5000;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _enterItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly ToolStripMenuItem _profileItem;
    private readonly Icon _offIcon;
    private readonly Icon _onIcon;

    public TrayIcon()
    {
        _offIcon = TrayIconImages.Create(desktopModeActive: false);
        _onIcon = TrayIconImages.Create(desktopModeActive: true);

        _enterItem = new ToolStripMenuItem("&Enter Desktop Mode", null, (_, _) => EnterRequested?.Invoke(this, EventArgs.Empty));
        _exitItem = new ToolStripMenuItem("E&xit Desktop Mode", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        _profileItem = new ToolStripMenuItem("&Profile");
        var settingsItem = new ToolStripMenuItem("&Settings...", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        var restartItem = new ToolStripMenuItem(
            "&Restart as administrator", null, (_, _) => RestartAsAdminRequested?.Invoke(this, EventArgs.Empty))
        {
            Visible = !ProcessElevation.IsElevated(),
        };
        var quitItem = new ToolStripMenuItem("&Quit", null, (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty));

        _menu = new ContextMenuStrip();
        _menu.Items.AddRange([_enterItem, _exitItem, _profileItem, settingsItem, restartItem, new ToolStripSeparator(), quitItem]);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = _offIcon,
            Text = AppName,
        };
        _notifyIcon.DoubleClick += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        ShowState(desktopModeActive: false, [], Guid.Empty);
        _notifyIcon.Visible = true;
    }

    public event EventHandler? EnterRequested;

    public event EventHandler? ExitRequested;

    /// <summary>The user picked a Profile from the menu; the argument is its id.</summary>
    public event EventHandler<Guid>? ProfileSelected;

    /// <summary>The user chose "Settings..." or double-clicked the icon.</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>Only raised when the process is not elevated (the item is hidden otherwise).</summary>
    public event EventHandler? RestartAsAdminRequested;

    public event EventHandler? QuitRequested;

    /// <summary>Updates icon, tooltip and menu to the current Desktop Mode state and Profiles.</summary>
    public void ShowState(bool desktopModeActive, IReadOnlyList<Profile> profiles, Guid activeProfileId)
    {
        _notifyIcon.Icon = desktopModeActive ? _onIcon : _offIcon;
        _enterItem.Enabled = !desktopModeActive;
        _exitItem.Enabled = desktopModeActive;

        var activeName = profiles.FirstOrDefault(p => p.Id == activeProfileId)?.Name;
        _notifyIcon.Text = ToolTipText(desktopModeActive, activeName);
        RebuildProfileItems(profiles, activeProfileId);
    }

    /// <summary>Shows <paramref name="text"/> in a notification next to the tray icon.</summary>
    public void ShowNotice(string text) =>
        _notifyIcon.ShowBalloonTip(NoticeTimeoutMilliseconds, AppName, text, ToolTipIcon.Info);

    public void Dispose()
    {
        // Hide first so the icon does not linger in the notification area until the mouse passes over it.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _offIcon.Dispose();
        _onIcon.Dispose();
    }

    private static string ToolTipText(bool desktopModeActive, string? activeProfileName)
    {
        var text = $"{AppName}{TitleSeparator}Desktop Mode: {(desktopModeActive ? "On" : "Off")}";
        if (activeProfileName is not null)
        {
            text += $"{TitleSeparator}Profile: {activeProfileName}";
        }

        return text.Length <= MaxToolTipLength
            ? text
            : string.Concat(text.AsSpan(0, MaxToolTipLength - Ellipsis.Length), Ellipsis);
    }

    /// <summary>Menu text treats a single '&amp;' as a mnemonic marker; Profile names show it literally.</summary>
    private static string EscapeMnemonic(string text) => text.Replace("&", "&&", StringComparison.Ordinal);

    private void RebuildProfileItems(IReadOnlyList<Profile> profiles, Guid activeProfileId)
    {
        foreach (var old in _profileItem.DropDownItems.Cast<ToolStripItem>().ToList())
        {
            old.Dispose();
        }

        foreach (var profile in profiles)
        {
            var id = profile.Id;
            var item = new ToolStripMenuItem(EscapeMnemonic(profile.Name), null, (_, _) => ProfileSelected?.Invoke(this, id))
            {
                Checked = id == activeProfileId,
            };
            _profileItem.DropDownItems.Add(item);
        }

        _profileItem.Enabled = profiles.Count > 0;
    }
}
