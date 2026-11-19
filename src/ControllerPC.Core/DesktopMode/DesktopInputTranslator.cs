using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Translates the Driving Controller's state into mouse and keyboard input while Desktop Mode is on,
/// according to one Profile.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Left stick always moves the cursor; the right stick follows the Profile's Right Stick Role.
/// Stick Y is up-positive, screen Y is down-positive: the translator does the flip.</item>
/// <item>Stick input inside the deadzone is ignored; the remaining range is rescaled to [0, 1].
/// Sub-pixel and sub-notch remainders carry over between updates.</item>
/// <item>Mouse button and Key Combination Actions are pressed when their button goes down and released when it goes up.
/// Keys and mouse buttons are reference-counted, so when two held Bindings share one (e.g. Ctrl), it is only released
/// after the last of them is let go.</item>
/// <item>Nudge Actions move once on press, then repeat while held after a delay, accelerating (named constants).</item>
/// <item>Profile switch Actions are returned from <see cref="Update"/> once per press; the caller acts on them after
/// <see cref="Update"/> returns.</item>
/// <item>Chord buttons, the buttons used in any of the Profile's Chord Bindings, wait before performing their own
/// Binding: it is pressed once the button has been held for the Profile's Chord Wait, or tapped (pressed and released
/// in the same update; a Nudge moves one step, a Profile switch fires once) when the button is released sooner.
/// A Chord Wait of zero performs it at once. Other buttons perform their Binding at once, as always.</item>
/// <item>A Chord Binding becomes active when a press completes its whole Chord. That cancels the waiting Bindings of its
/// buttons, releases those already performed (e.g. a left click held for a drag), and releases any active Chord Binding
/// that is a strict subset of it; a Chord Binding that is a strict subset of an active one does not activate. Chord
/// Bindings of equal size that overlap can both be active. Its Action is held until any of its buttons is released.
/// The buttons of an activated Chord are consumed: they perform no Binding of their own until released and pressed
/// again.</item>
/// <item>Chord Bindings and the Chord Wait are read when <see cref="Begin"/> is called; the Chord Wait is timed with
/// the clamped <c>elapsed</c>.</item>
/// <item>Buttons held when <see cref="Begin"/> is called are ignored until released, and cannot complete a Chord.</item>
/// <item><see cref="Update"/> before <see cref="Begin"/> (or after <see cref="ReleaseAll"/>) does nothing.</item>
/// </list>
/// Time is passed as <c>elapsed</c> since the previous update because movement integrates speed over time.
/// <c>elapsed</c> is clamped to [0, 100 ms], so a clock going backwards never moves the cursor in reverse and a
/// stalled poll loop does not make the cursor jump or held Nudges catch up all at once.
/// Not thread-safe: call from a single thread.
/// </remarks>
public sealed class DesktopInputTranslator
{
    /// <summary>The longest time one update integrates movement and Nudge repeats over.</summary>
    internal static readonly TimeSpan MaxUpdateElapsed = TimeSpan.FromMilliseconds(100);

    private readonly ButtonEdgeDetector _buttonEdges = new();
    private readonly HeldOutputTracker _heldOutput;
    private readonly NudgeRepeater _nudges;
    private readonly CursorMover _cursor;
    private readonly Scroller _scroller;

    private readonly PendingSingleBindings _pending = new();
    private readonly ChordBindingResolver _chordBindings = new();

    // The Action each held button (or active Chord Binding, keyed by its whole Chord) produced when pressed, so its
    // release matches its press even if the Profile's Bindings are edited in between.
    private readonly Dictionary<ControllerButton, ButtonAction> _heldActions = [];

    private Profile? _profile;
    private TimeSpan _chordWait;

    public DesktopInputTranslator(IInputSink sink)
    {
        _heldOutput = new HeldOutputTracker(sink);
        _nudges = new NudgeRepeater(sink);
        _cursor = new CursorMover(sink);
        _scroller = new Scroller(sink);
    }

    /// <summary>Starts translating with <paramref name="profile"/>. Calls <see cref="ReleaseAll"/> first.</summary>
    public void Begin(Profile profile, ControllerButton heldAtStart)
    {
        ReleaseAll();
        _buttonEdges.Begin(heldAtStart);
        _chordBindings.Begin(profile.ChordBindings);
        _chordWait = profile.ChordWait;
        _profile = profile;
    }

    /// <summary>
    /// Processes one poll of the Driving Controller; <paramref name="elapsed"/> is the time since the previous update.
    /// Returns the first Profile switch Action performed during this update, if any. They are performed in this order:
    /// taps of released Chord buttons, presses of other buttons, Chord Bindings that became active, then Chord buttons
    /// whose Chord Wait ran out; buttons within each in <see cref="ControllerButtons.All"/> order.
    /// </summary>
    public ProfileSwitchAction? Update(ControllerState state, TimeSpan elapsed)
    {
        if (_profile is null)
        {
            return null;
        }

        var boundedElapsed = TimeSpan.FromTicks(Math.Clamp(elapsed.Ticks, 0, MaxUpdateElapsed.Ticks));
        var edges = _buttonEdges.Update(state.Buttons);

        // Releases go first so that a Binding let go in the same poll another is pressed is fully up before the new one
        // goes down; held Nudges and waiting buttons advance before new presses so a fresh press does not count this
        // update's time.
        var profileSwitch = ReleaseButtons(edges.Released, _profile);
        _nudges.Advance(boundedElapsed);
        _pending.Advance(boundedElapsed);
        profileSwitch = KeepFirst(profileSwitch, PressButtons(edges.Pressed, _profile));
        profileSwitch = KeepFirst(profileSwitch, ActivateChordBindings(edges, _profile));
        foreach (var button in _pending.TakeDue(_chordWait))
        {
            profileSwitch = KeepFirst(profileSwitch, PressAction(button, _profile.GetAction(button), _profile));
        }

        MoveWithSticks(state, _profile, boundedElapsed);
        return profileSwitch;
    }

    /// <summary>Releases every key and mouse button currently held through the sink and stops translating until the next <see cref="Begin"/>.</summary>
    public void ReleaseAll()
    {
        _heldOutput.ReleaseAll();
        _heldActions.Clear();
        _pending.Clear();
        _chordBindings.Clear();
        _nudges.Clear();
        _cursor.Reset();
        _scroller.Reset();
        _profile = null;
    }

    private ProfileSwitchAction? ReleaseButtons(ControllerButton released, Profile profile)
    {
        foreach (var chordBinding in _chordBindings.Release(released))
        {
            ReleaseAction(chordBinding.Chord.Buttons);
        }

        ProfileSwitchAction? profileSwitch = null;
        foreach (var button in ControllerButtons.Split(released))
        {
            if (_pending.Remove(button))
            {
                // Let go within the Chord Wait: a tap.
                profileSwitch = KeepFirst(profileSwitch, PressAction(button, profile.GetAction(button), profile));
            }

            // A consumed button holds nothing of its own, so this releases nothing for it.
            ReleaseAction(button);
        }

        return profileSwitch;
    }

    private ProfileSwitchAction? PressButtons(ControllerButton pressed, Profile profile)
    {
        ProfileSwitchAction? profileSwitch = null;
        foreach (var button in ControllerButtons.Split(pressed))
        {
            if ((button & _chordBindings.ChordButtons) != ControllerButton.None)
            {
                _pending.Add(button);
            }
            else
            {
                profileSwitch = KeepFirst(profileSwitch, PressAction(button, profile.GetAction(button), profile));
            }
        }

        return profileSwitch;
    }

    private ProfileSwitchAction? ActivateChordBindings(ButtonEdges edges, Profile profile)
    {
        ProfileSwitchAction? profileSwitch = null;
        foreach (var activation in _chordBindings.Activate(edges.Held, edges.Pressed))
        {
            foreach (var displaced in activation.Displaced)
            {
                ReleaseAction(displaced.Chord.Buttons);
            }

            var chord = activation.Activated.Chord.Buttons;
            foreach (var button in ControllerButtons.Split(chord))
            {
                _pending.Remove(button);
                ReleaseAction(button);
            }

            profileSwitch = KeepFirst(profileSwitch, PressAction(chord, activation.Activated.Action, profile));
        }

        return profileSwitch;
    }

    /// <summary>
    /// Presses <paramref name="action"/> for <paramref name="key"/> (one button, or a whole Chord) and keeps it held
    /// until <see cref="ReleaseAction"/>.
    /// </summary>
    /// <returns><paramref name="action"/> when it is a Profile switch, which is performed once instead of held.</returns>
    private ProfileSwitchAction? PressAction(ControllerButton key, ButtonAction action, Profile profile)
    {
        switch (action)
        {
            case MouseButtonAction mouse:
                _heldOutput.PressMouseButton(mouse.Button);
                break;
            case KeyCombinationAction keys:
                _heldOutput.PressKeys(keys.Keys);
                break;
            case NudgeAction nudge:
                _nudges.Press(key, nudge.Direction, profile.Tuning.NudgeStep);
                break;
            case ProfileSwitchAction profileSwitch:
                return profileSwitch;
            default:
                return null;
        }

        _heldActions[key] = action;
        return null;
    }

    private void ReleaseAction(ControllerButton key)
    {
        if (!_heldActions.Remove(key, out var action))
        {
            return;
        }

        switch (action)
        {
            case MouseButtonAction mouse:
                _heldOutput.ReleaseMouseButton(mouse.Button);
                break;
            case KeyCombinationAction keys:
                _heldOutput.ReleaseKeys(keys.Keys);
                break;
            case NudgeAction:
                _nudges.Release(key);
                break;
        }
    }

    /// <summary>Keeps the first Profile switch of an update; unlike <c>??=</c>, the second argument is always evaluated.</summary>
    private static ProfileSwitchAction? KeepFirst(ProfileSwitchAction? first, ProfileSwitchAction? next) => first ?? next;

    private void MoveWithSticks(ControllerState state, Profile profile, TimeSpan elapsed)
    {
        var tuning = profile.Tuning;
        var cursorDeflection = StickCurve.Shape(state.LeftStick, tuning.Deadzone, tuning.AccelerationEnabled);

        switch (profile.RightStickRole)
        {
            case RightStickRole.Cursor:
            {
                var right = StickCurve.Shape(state.RightStick, tuning.Deadzone, tuning.AccelerationEnabled);
                cursorDeflection = new StickPosition(cursorDeflection.X + right.X, cursorDeflection.Y + right.Y);
                break;
            }

            case RightStickRole.Scroll:
            {
                // Acceleration is a cursor setting; scrolling stays linear so its speed is predictable.
                var scrollDeflection = StickCurve.Shape(state.RightStick, tuning.Deadzone, accelerationEnabled: false);
                _scroller.Scroll(scrollDeflection, tuning.ScrollSpeed, elapsed);
                break;
            }
        }

        _cursor.Move(cursorDeflection, tuning.CursorSpeed, elapsed);
    }
}
