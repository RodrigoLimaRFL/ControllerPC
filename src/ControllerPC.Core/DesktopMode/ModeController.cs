using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Switches Desktop Mode on and off from the Mode Triggers (or the tray), keeps track of the Driving Controller
/// and the Active Profile, and feeds the Driving Controller to a <see cref="DesktopInputTranslator"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Outside Desktop Mode, every connected slot is watched for each of <see cref="AppSettings.WatchedEntryTriggers"/>:
/// every Entry Trigger Override, plus the Base Entry Trigger when the Active Profile has no override. The first to fire
/// (slots in order, then watched order) wins: its Profile becomes the Active Profile, its slot the Driving Controller,
/// and Desktop Mode is entered with <see cref="RumblePattern.EnteredDesktopMode"/> on that slot.</item>
/// <item>Entering, switching Profile, resuming and applying settings all restart translation and the Exit Trigger with
/// the buttons held at that moment, which must be released before they act. So identical Entry and Exit Triggers do not
/// bounce back and forth, and likewise the Entry Triggers after leaving. A slot that (re)connects must also release
/// what it holds before it can fire an Entry Trigger, including at start-up.</item>
/// <item>In Desktop Mode only the Driving Controller is read. Each <see cref="Tick"/> first feeds the Exit Trigger of the
/// Active Profile; if it fires, every held key and mouse button is released and Desktop Mode is left with
/// <see cref="RumblePattern.ExitedDesktopMode"/>. Otherwise the state is translated, and a Profile switch Action it
/// returns is acted on afterwards: Next Profile goes to the next Profile in list order, wrapping; Switch to Profile
/// goes to that Profile if it still exists. Switching plays <see cref="RumblePattern.ProfileSwitched"/>; switching to the
/// Active Profile does nothing.</item>
/// <item>If reading the Driving Controller fails, everything held is released and Desktop Mode is left (no rumble).
/// The source reports a disconnect and any other read error alike, as <c>null</c>, so a single failed read is enough to
/// leave Desktop Mode: that way no key or mouse button can stay held on a controller that cannot be read.</item>
/// <item>The first translated Tick after entering or resuming has no elapsed time; after that, elapsed is the time
/// since the previous Tick.</item>
/// <item>A slot found disconnected is re-probed at most once per second (except by <see cref="EnterDesktopMode"/>);
/// connected slots are read every Tick.</item>
/// <item>While paused (e.g. while a Mode Trigger is being recorded) everything held is released and no Mode Trigger is
/// matched and nothing is translated; Desktop Mode stays as it is, and only a Driving Controller disconnect is handled.
/// </item>
/// </list>
/// <see cref="StateChanged"/> is raised synchronously, on the calling thread, whenever Desktop Mode, the Driving
/// Controller, the Active Profile or the pause changes, and after <see cref="ApplySettings"/>.
/// Time is passed as an absolute, monotonic <c>now</c>. Not thread-safe: call every member from one thread.
/// </remarks>
public sealed class ModeController
{
    private readonly IRumble _rumble;
    private readonly SlotProber _slots;
    private readonly DesktopInputTranslator _translator;

    private EntryWatcher _entryWatcher;
    private DrivingControllerSession? _session;

    /// <exception cref="ArgumentException"><paramref name="settings"/> contains no Profiles.</exception>
    public ModeController(IControllerSource source, IRumble rumble, IInputSink sink, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(rumble);
        ArgumentNullException.ThrowIfNull(sink);
        ThrowIfNoProfiles(settings);

        _rumble = rumble;
        _slots = new SlotProber(source);
        _translator = new DesktopInputTranslator(sink);
        Settings = settings;
        _entryWatcher = CreateEntryWatcher();
    }

    /// <summary>Raised when Desktop Mode, the Driving Controller, the Active Profile or the pause changes.</summary>
    public event EventHandler? StateChanged;

    public bool IsDesktopModeActive => _session is not null;

    /// <summary>The slot of the Driving Controller; <c>null</c> outside Desktop Mode.</summary>
    public int? DrivingSlot => _session?.Slot;

    /// <summary>Whether matching and translation are paused (see <see cref="Pause"/>).</summary>
    public bool IsPaused { get; private set; }

    /// <summary>The live settings; <see cref="AppSettings.ActiveProfileId"/> follows the Active Profile.</summary>
    public AppSettings Settings { get; private set; }

    public Profile ActiveProfile => Settings.ActiveProfile;

    /// <summary>Processes one poll of the controllers.</summary>
    public void Tick(TimeSpan now)
    {
        if (_session is { } session)
        {
            TickInDesktopMode(session, now);
        }
        else if (!IsPaused)
        {
            TickOutsideDesktopMode(now);
        }
    }

    /// <summary>
    /// Enters Desktop Mode by hand, with the first connected slot as Driving Controller. Every slot is read, including
    /// one found disconnected less than a re-probe interval ago, so a controller plugged in just now is found.
    /// Returns <c>false</c> (and does nothing) when no controller is connected, Desktop Mode is already on, or paused.
    /// </summary>
    public bool EnterDesktopMode(TimeSpan now)
    {
        if (_session is not null || IsPaused)
        {
            return false;
        }

        for (var slot = 0; slot < _slots.SlotCount; slot++)
        {
            // Probed regardless of the re-probe interval: the user may have just plugged the controller in.
            if (_slots.Probe(slot, now) is { } state)
            {
                Enter(slot, state.Buttons);
                return true;
            }
        }

        return false;
    }

    /// <summary>Leaves Desktop Mode by hand, as the Exit Trigger would. Does nothing outside Desktop Mode.</summary>
    public void ExitDesktopMode(TimeSpan now)
    {
        if (_session is { } session)
        {
            Leave(session, now, rumble: true);
        }
    }

    /// <summary>
    /// Makes the Profile with <paramref name="profileId"/> the Active Profile; an unknown id or the Active Profile's own
    /// is ignored. In Desktop Mode this plays <see cref="RumblePattern.ProfileSwitched"/> on the Driving Controller.
    /// </summary>
    public void SetActiveProfile(Guid profileId, TimeSpan now)
    {
        if (Settings.FindProfile(profileId) is not { } profile || profile == ActiveProfile)
        {
            return;
        }

        MakeActive(profile);
        if (_session is { } session)
        {
            _rumble.Play(session.Slot, RumblePattern.ProfileSwitched);
        }

        RestartMatchingAndTranslation(now);
        RaiseStateChanged();
    }

    /// <summary>
    /// Switches to newly saved <paramref name="settings"/>. The live Active Profile stays active if it still exists
    /// in them (its id is written into <paramref name="settings"/>); otherwise their own Active Profile applies.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="settings"/> contains no Profiles; nothing is changed, the current settings stay in effect.
    /// </exception>
    public void ApplySettings(AppSettings settings, TimeSpan now)
    {
        ThrowIfNoProfiles(settings);

        var liveActiveProfileId = ActiveProfile.Id;
        if (settings.FindProfile(liveActiveProfileId) is not null)
        {
            settings.ActiveProfileId = liveActiveProfileId;
        }

        Settings = settings;
        _entryWatcher = CreateEntryWatcher();
        RestartMatchingAndTranslation(now);
        RaiseStateChanged();
    }

    /// <summary>
    /// Releases everything held and stops matching and translating until <see cref="Resume"/>; Desktop Mode stays as it is.
    /// </summary>
    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        IsPaused = true;
        _translator.ReleaseAll();
        RaiseStateChanged();
    }

    /// <summary>Resumes after <see cref="Pause"/>; buttons held now must be released before they act.</summary>
    public void Resume(TimeSpan now)
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        if (_session is { } session)
        {
            session.LastTranslatedAt = null;
        }

        RestartMatchingAndTranslation(now);
        RaiseStateChanged();
    }

    private void TickOutsideDesktopMode(TimeSpan now)
    {
        for (var slot = 0; slot < _slots.SlotCount; slot++)
        {
            var wasConnected = _slots.IsConnected(slot);
            if (_slots.Read(slot, now) is not { } state)
            {
                continue;
            }

            if (!wasConnected)
            {
                _entryWatcher.Reset(slot, state.Buttons);
                continue;
            }

            if (_entryWatcher.Update(slot, state.Buttons, now) is { } fired)
            {
                MakeActive(fired.Profile);
                Enter(slot, state.Buttons);
                return;
            }
        }
    }

    private void TickInDesktopMode(DrivingControllerSession session, TimeSpan now)
    {
        if (_slots.Read(session.Slot, now) is not { } state)
        {
            Leave(session, now, rumble: false);
            return;
        }

        if (IsPaused)
        {
            return;
        }

        if (session.ExitMatcher.Update(state.Buttons, now))
        {
            Leave(session, now, rumble: true);
            return;
        }

        var elapsed = session.LastTranslatedAt is { } previous ? now - previous : TimeSpan.Zero;
        session.LastTranslatedAt = now;
        if (_translator.Update(state, elapsed) is { } profileSwitch)
        {
            SwitchProfile(session, profileSwitch, state.Buttons);
        }
    }

    private void SwitchProfile(DrivingControllerSession session, ProfileSwitchAction action, ControllerButton held)
    {
        var target = action switch
        {
            NextProfileAction => NextProfile(),
            SwitchToProfileAction switchTo => Settings.FindProfile(switchTo.ProfileId),
            _ => null,
        };
        if (target is null || target == ActiveProfile)
        {
            return;
        }

        MakeActive(target);
        BeginTranslating(session, held);
        _rumble.Play(session.Slot, RumblePattern.ProfileSwitched);
        RaiseStateChanged();
    }

    private Profile NextProfile()
    {
        var profiles = Settings.Profiles;
        var index = profiles.IndexOf(ActiveProfile);
        return profiles[(index + 1) % profiles.Count];
    }

    private void Enter(int slot, ControllerButton held)
    {
        var session = new DrivingControllerSession(slot, CreateExitMatcher(held));
        _session = session;
        _translator.Begin(ActiveProfile, held);
        _rumble.Play(slot, RumblePattern.EnteredDesktopMode);
        RaiseStateChanged();
    }

    private void Leave(DrivingControllerSession session, TimeSpan now, bool rumble)
    {
        _translator.ReleaseAll();
        _session = null;
        if (rumble)
        {
            _rumble.Play(session.Slot, RumblePattern.ExitedDesktopMode);
        }

        ResetEntryWatcher(now);
        RaiseStateChanged();
    }

    /// <summary>Makes <paramref name="profile"/> active; the watched Entry Triggers depend on it, so they are rebuilt.</summary>
    private void MakeActive(Profile profile)
    {
        Settings.ActiveProfileId = profile.Id;
        _entryWatcher = CreateEntryWatcher();
    }

    /// <summary>
    /// After the Active Profile, settings or pause changed: restarts whatever currently runs (translation and the Exit
    /// Trigger in Desktop Mode, the Entry Triggers outside it) with the buttons held now.
    /// </summary>
    private void RestartMatchingAndTranslation(TimeSpan now)
    {
        if (IsPaused)
        {
            return;
        }

        if (_session is not { } session)
        {
            ResetEntryWatcher(now);
        }
        else if (_slots.Read(session.Slot, now) is { } state)
        {
            BeginTranslating(session, state.Buttons);
        }
        else
        {
            Leave(session, now, rumble: false);
        }
    }

    private void BeginTranslating(DrivingControllerSession session, ControllerButton held)
    {
        _translator.Begin(ActiveProfile, held);
        session.ExitMatcher = CreateExitMatcher(held);
    }

    private void ResetEntryWatcher(TimeSpan now)
    {
        for (var slot = 0; slot < _slots.SlotCount; slot++)
        {
            var held = _slots.Read(slot, now)?.Buttons ?? ControllerButton.None;
            _entryWatcher.Reset(slot, held);
        }
    }

    private EntryWatcher CreateEntryWatcher() => new(Settings.WatchedEntryTriggers(), _slots.SlotCount);

    private TriggerMatcher CreateExitMatcher(ControllerButton held) =>
        new(Settings.EffectiveExitTrigger(ActiveProfile), held);

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>The Active Profile, and so everything the controller does, needs at least one Profile.</summary>
    private static void ThrowIfNoProfiles(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Profiles.Count == 0)
        {
            throw new ArgumentException("Settings must contain at least one Profile.", nameof(settings));
        }
    }
}
