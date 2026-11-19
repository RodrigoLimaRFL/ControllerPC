namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Tracks when a held Nudge repeats: first after <see cref="InitialRepeatDelay"/>, then at an interval that
/// starts at <see cref="FirstRepeatInterval"/> and shrinks by <see cref="RepeatIntervalShrinkFactor"/> with every
/// repeat, down to <see cref="MinimumRepeatInterval"/>.
/// </summary>
internal sealed class NudgeRepeatTimer
{
    /// <summary>Long enough that a quick tap produces exactly one Nudge.</summary>
    internal static readonly TimeSpan InitialRepeatDelay = TimeSpan.FromMilliseconds(400);

    internal static readonly TimeSpan FirstRepeatInterval = TimeSpan.FromMilliseconds(100);

    internal const double RepeatIntervalShrinkFactor = 0.8;

    /// <summary>Fastest repeat rate (50 Nudges per second), reached after about a second of holding.</summary>
    internal static readonly TimeSpan MinimumRepeatInterval = TimeSpan.FromMilliseconds(20);

    private TimeSpan _untilNextRepeat = InitialRepeatDelay;
    private TimeSpan _repeatInterval = FirstRepeatInterval;

    /// <summary>Advances the timer by <paramref name="elapsed"/> and returns how many repeats fell due.</summary>
    public int Advance(TimeSpan elapsed)
    {
        var repeats = 0;
        var remaining = elapsed;
        while (remaining >= _untilNextRepeat)
        {
            remaining -= _untilNextRepeat;
            repeats++;
            _untilNextRepeat = _repeatInterval;
            _repeatInterval = TimeSpan.FromTicks(Math.Max(
                MinimumRepeatInterval.Ticks,
                (long)(_repeatInterval.Ticks * RepeatIntervalShrinkFactor)));
        }

        _untilNextRepeat -= remaining;
        return repeats;
    }
}
