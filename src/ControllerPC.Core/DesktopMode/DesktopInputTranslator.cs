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
    private readonly IInputSink _sink;

    public DesktopInputTranslator(IInputSink sink)
    {
        _sink = sink;
    }

    /// <summary>Starts translating with <paramref name="profile"/>. Calls <see cref="ReleaseAll"/> first.</summary>
    public void Begin(Profile profile, ControllerButton heldAtStart) => throw new NotImplementedException();

    /// <summary>
    /// Processes one poll of the Driving Controller; <paramref name="elapsed"/> is the time since the previous update.
    /// Returns the Profile switch Action pressed during this update, if any (the first one, in
    /// <see cref="ControllerButtons.All"/> order).
    /// </summary>
    public ProfileSwitchAction? Update(ControllerState state, TimeSpan elapsed) => throw new NotImplementedException();

    /// <summary>Releases every key and mouse button currently held through the sink and stops translating until the next <see cref="Begin"/>.</summary>
    public void ReleaseAll() => throw new NotImplementedException();
}
