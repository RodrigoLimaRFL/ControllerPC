using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.Engine;

/// <summary>
/// Translates the Driving Controller's state into mouse and keyboard input while Desktop Mode is on,
/// according to one Profile.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Left stick always moves the cursor; the right stick follows the Profile's Right Stick Role.</item>
/// <item>Stick input inside the deadzone is ignored; sub-pixel and sub-notch remainders carry over between updates.</item>
/// <item>Mouse button and Key Combination Actions are pressed when their button goes down and released when it goes up.</item>
/// <item>Nudge Actions move once on press, then repeat while held, accelerating.</item>
/// <item>Profile switch Actions raise <see cref="ProfileSwitchRequested"/> once per press.</item>
/// <item>Buttons held when <see cref="Begin"/> is called are ignored until released.</item>
/// </list>
/// Not thread-safe: call from a single thread.
/// </remarks>
public sealed class DesktopInputTranslator
{
    private readonly IInputSink _sink;

    public DesktopInputTranslator(IInputSink sink)
    {
        _sink = sink;
    }

    /// <summary>Raised when a pressed button's Action is a <see cref="ProfileSwitchAction"/>.</summary>
#pragma warning disable CS0067 // Contract stub: raised once implemented in feature/input-translation.
    public event Action<ProfileSwitchAction>? ProfileSwitchRequested;
#pragma warning restore CS0067

    /// <summary>Starts translating with <paramref name="profile"/>. Calls <see cref="ReleaseAll"/> first.</summary>
    public void Begin(Profile profile, ControllerButton heldAtStart) => throw new NotImplementedException();

    /// <summary>Processes one poll of the Driving Controller; <paramref name="elapsed"/> is the time since the previous update.</summary>
    public void Update(ControllerState state, TimeSpan elapsed) => throw new NotImplementedException();

    /// <summary>Releases every key and mouse button currently held through the sink and stops translating until the next <see cref="Begin"/>.</summary>
    public void ReleaseAll() => throw new NotImplementedException();
}
