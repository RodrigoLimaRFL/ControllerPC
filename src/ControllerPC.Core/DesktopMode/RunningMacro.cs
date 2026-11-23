using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// One run of a Macro: holds each Key Combination for <see cref="TapDuration"/>, releases it, then waits the Macro Delay
/// before pressing the next.
/// </summary>
internal sealed class RunningMacro
{
    /// <summary>
    /// How long each Key Combination is held: long enough for applications that poll the keyboard to see it,
    /// short enough to feel instant.
    /// </summary>
    internal static readonly TimeSpan TapDuration = TimeSpan.FromMilliseconds(30);

    private readonly MacroAction _macro;
    private readonly HeldOutputTracker _heldOutput;

    private int _index;
    private bool _isHoldingKeys;
    private TimeSpan _untilNextChange;

    private RunningMacro(MacroAction macro, HeldOutputTracker heldOutput)
    {
        _macro = macro;
        _heldOutput = heldOutput;
    }

    public bool IsFinished { get; private set; }

    /// <summary>Starts a run of <paramref name="macro"/>, which must have a Key Combination, by pressing the first one.</summary>
    public static RunningMacro Start(MacroAction macro, HeldOutputTracker heldOutput)
    {
        var run = new RunningMacro(macro, heldOutput);
        run.PressCurrent();
        return run;
    }

    /// <summary>Performs every press and release that falls due within <paramref name="elapsed"/>.</summary>
    public void Advance(TimeSpan elapsed)
    {
        var remaining = elapsed;
        while (!IsFinished && remaining >= _untilNextChange)
        {
            remaining -= _untilNextChange;
            if (_isHoldingKeys)
            {
                ReleaseCurrent();
            }
            else
            {
                PressCurrent();
            }
        }

        if (!IsFinished)
        {
            _untilNextChange -= remaining;
        }
    }

    /// <summary>Stops the run at once, releasing the Key Combination it holds, if any.</summary>
    public void Abort()
    {
        if (_isHoldingKeys)
        {
            _heldOutput.ReleaseKeys(_macro.KeyCombinations[_index]);
            _isHoldingKeys = false;
        }

        IsFinished = true;
    }

    private void PressCurrent()
    {
        _heldOutput.PressKeys(_macro.KeyCombinations[_index]);
        _isHoldingKeys = true;
        _untilNextChange = TapDuration;
    }

    private void ReleaseCurrent()
    {
        _heldOutput.ReleaseKeys(_macro.KeyCombinations[_index]);
        _isHoldingKeys = false;
        _index++;
        if (_index == _macro.KeyCombinations.Length)
        {
            IsFinished = true;
            return;
        }

        // A negative Macro Delay fails validation; treat it as none rather than waiting backwards.
        _untilNextChange = _macro.Delay > TimeSpan.Zero ? _macro.Delay : TimeSpan.Zero;
    }
}
