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
/// <item>Buttons held when <see cref="Begin"/> is called are ignored until released.</item>
/// <item><see cref="Update"/> before <see cref="Begin"/> (or after <see cref="ReleaseAll"/>) does nothing.</item>
/// </list>
/// Time is passed as <c>elapsed</c> since the previous update because movement integrates speed over time.
/// Not thread-safe: call from a single thread.
/// </remarks>
public sealed class DesktopInputTranslator
{
    private readonly ButtonEdgeDetector _buttonEdges = new();
    private readonly HeldOutputTracker _heldOutput;
    private readonly NudgeRepeater _nudges;
    private readonly CursorMover _cursor;
    private readonly Scroller _scroller;

    // The Action each held button produced when pressed, so its release matches its press
    // even if the Profile's Bindings are edited in between.
    private readonly Dictionary<ControllerButton, ButtonAction> _heldActions = [];

    private Profile? _profile;

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
        _profile = profile;
    }

    /// <summary>
    /// Processes one poll of the Driving Controller; <paramref name="elapsed"/> is the time since the previous update.
    /// Returns the Profile switch Action pressed during this update, if any (the first one, in
    /// <see cref="ControllerButtons.All"/> order).
    /// </summary>
    public ProfileSwitchAction? Update(ControllerState state, TimeSpan elapsed)
    {
        if (_profile is null)
        {
            return null;
        }

        var edges = _buttonEdges.Update(state.Buttons);

        // Releases go first so that a Binding let go in the same poll another is pressed is fully up before
        // the new one goes down; held Nudges advance before new presses so a fresh press does not repeat at once.
        foreach (var button in ControllerButtons.Split(edges.Released))
        {
            ReleaseBinding(button);
        }

        _nudges.Advance(elapsed);

        ProfileSwitchAction? profileSwitch = null;
        foreach (var button in ControllerButtons.Split(edges.Pressed))
        {
            profileSwitch ??= PressBinding(button, _profile);
        }

        MoveWithSticks(state, _profile, elapsed);
        return profileSwitch;
    }

    /// <summary>Releases every key and mouse button currently held through the sink and stops translating until the next <see cref="Begin"/>.</summary>
    public void ReleaseAll()
    {
        _heldOutput.ReleaseAll();
        _heldActions.Clear();
        _nudges.Clear();
        _cursor.Reset();
        _scroller.Reset();
        _profile = null;
    }

    /// <returns>The Profile switch Action bound to <paramref name="button"/>, if that is what it is bound to.</returns>
    private ProfileSwitchAction? PressBinding(ControllerButton button, Profile profile)
    {
        var action = profile.GetAction(button);
        switch (action)
        {
            case MouseButtonAction mouse:
                _heldOutput.PressMouseButton(mouse.Button);
                break;
            case KeyCombinationAction keys:
                _heldOutput.PressKeys(keys.Keys);
                break;
            case NudgeAction nudge:
                _nudges.Press(button, nudge.Direction, profile.Tuning.NudgeStep);
                break;
            case ProfileSwitchAction profileSwitch:
                return profileSwitch;
            default:
                return null;
        }

        _heldActions[button] = action;
        return null;
    }

    private void ReleaseBinding(ControllerButton button)
    {
        if (!_heldActions.Remove(button, out var action))
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
                _nudges.Release(button);
                break;
        }
    }

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
