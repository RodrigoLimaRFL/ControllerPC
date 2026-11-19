using System.Collections.Immutable;
using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Triggers;

/// <summary>
/// Records a Mode Trigger from live controller input: every group of buttons pressed between "nothing held"
/// moments becomes one Step whose Chord is the union of the buttons held during that group.
/// </summary>
/// <remarks>
/// Buttons ignored since <see cref="Start"/> are invisible until released: they neither join a group nor count as held.
/// Not thread-safe: call from a single thread.
/// </remarks>
public sealed class TriggerRecorder
{
    private ImmutableList<Chord> _steps = [];
    private ControllerButton _ignored;
    private ControllerButton _currentGroup;
    private TimeSpan _lastGroupReleasedAt;

    /// <summary>Starts a new recording, discarding any previous one. Buttons held right now are ignored until released.</summary>
    public void Start(ControllerButton heldNow, TimeSpan now)
    {
        _steps = [];
        _ignored = heldNow;
        _currentGroup = ControllerButton.None;
    }

    /// <summary>Feeds the currently held buttons.</summary>
    public void Update(ControllerButton held, TimeSpan now)
    {
        _ignored &= held;
        var contributing = held & ~_ignored;

        if (contributing != ControllerButton.None)
        {
            _currentGroup |= contributing;
            return;
        }

        if (_currentGroup != ControllerButton.None)
        {
            _steps = _steps.Add(new Chord(_currentGroup));
            _currentGroup = ControllerButton.None;
            _lastGroupReleasedAt = now;
        }
    }

    /// <summary>
    /// The Steps recorded so far (completed groups only), as a snapshot that later updates and <see cref="Start"/>
    /// do not change.
    /// </summary>
    public IReadOnlyList<Chord> Steps => _steps;

    /// <summary>True when at least one Step was recorded and nothing has been held for at least <paramref name="idle"/>.</summary>
    public bool IsIdleFor(TimeSpan idle, TimeSpan now) =>
        _steps.Count > 0
        && _currentGroup == ControllerButton.None
        && now - _lastGroupReleasedAt >= idle;
}
