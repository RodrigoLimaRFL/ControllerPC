using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Runs Macros: at most one run per source (a single button, or a whole Chord for a Chord Binding). Starting a Macro
/// whose source is still running is ignored; a run continues after its button is released. Keys go through the
/// <see cref="HeldOutputTracker"/>, so a key another Binding holds stays down.
/// </summary>
internal sealed class MacroRunner
{
    private readonly HeldOutputTracker _heldOutput;
    private readonly Dictionary<ControllerButton, RunningMacro> _running = [];

    public MacroRunner(HeldOutputTracker heldOutput)
    {
        _heldOutput = heldOutput;
    }

    /// <summary>Starts <paramref name="macro"/> for <paramref name="source"/>, pressing its first Key Combination now.</summary>
    public void Start(ControllerButton source, MacroAction macro)
    {
        if (_running.ContainsKey(source) || macro.KeyCombinations.IsEmpty)
        {
            return;
        }

        _running[source] = RunningMacro.Start(macro, _heldOutput);
    }

    public void Advance(TimeSpan elapsed)
    {
        foreach (var (source, run) in _running.ToList())
        {
            run.Advance(elapsed);
            if (run.IsFinished)
            {
                _running.Remove(source);
            }
        }
    }

    /// <summary>Stops every run at once, releasing the keys they hold.</summary>
    public void AbortAll()
    {
        foreach (var run in _running.Values)
        {
            run.Abort();
        }

        _running.Clear();
    }
}
