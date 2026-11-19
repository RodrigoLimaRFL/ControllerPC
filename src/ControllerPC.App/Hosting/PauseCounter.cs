namespace ControllerPC.App.Hosting;

/// <summary>
/// Counts overlapping pause requests (e.g. a Mode Trigger recording and a restart as administrator), so Desktop Mode
/// resumes only when the last of them ends.
/// </summary>
internal sealed class PauseCounter
{
    private int _count;

    public bool IsPaused => _count > 0;

    /// <summary>Adds a pause request; true when it is the first, i.e. the caller should pause now.</summary>
    public bool Pause() => ++_count == 1;

    /// <summary>
    /// Ends one pause request; true when it was the last, i.e. the caller should resume now. Extra calls with
    /// nothing paused are ignored.
    /// </summary>
    public bool Resume()
    {
        if (_count == 0)
        {
            return false;
        }

        return --_count == 0;
    }
}
