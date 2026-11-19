using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Triggers;

/// <summary>
/// Records a Mode Trigger from live controller input: every group of buttons pressed between "nothing held"
/// moments becomes one Step whose Chord is the union of the buttons held during that group.
/// </summary>
public sealed class TriggerRecorder
{
    /// <summary>Starts a new recording, discarding any previous one. Buttons held right now are ignored until released.</summary>
    public void Start(ControllerButton heldNow, TimeSpan now) => throw new NotImplementedException();

    /// <summary>Feeds the currently held buttons.</summary>
    public void Update(ControllerButton held, TimeSpan now) => throw new NotImplementedException();

    /// <summary>The Steps recorded so far (completed groups only).</summary>
    public IReadOnlyList<Chord> Steps => throw new NotImplementedException();

    /// <summary>True when at least one Step was recorded and nothing has been held for at least <paramref name="idle"/>.</summary>
    public bool IsIdleFor(TimeSpan idle, TimeSpan now) => throw new NotImplementedException();
}
