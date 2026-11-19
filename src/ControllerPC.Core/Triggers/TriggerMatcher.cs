using System.Diagnostics;
using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Triggers;

/// <summary>
/// Watches a stream of held-button sets and reports when one <see cref="ModeTrigger"/> is completed.
/// </summary>
/// <remarks>
/// Matching rules:
/// <list type="bullet">
/// <item>A Step completes the moment exactly its Chord's buttons are held; press order within the Chord does not matter.
/// Holding a strict subset of the expected Chord is a Chord being built up, not a break.</item>
/// <item>Holding any button outside the expected Chord breaks the match and resets the attempt.</item>
/// <item>The next Step only starts counting after every button of the previous Step is released.</item>
/// <item>If the next Step is not completed within the Step Gap of the previous Step's release, the attempt resets.
/// The first Step has no time limit.</item>
/// <item>With Final Hold enabled, the last Chord must stay held (exactly) for the Final Hold duration, counted from the
/// moment it became exactly held; releasing or adding a button before then resets the attempt.</item>
/// <item>Buttons already held when the matcher is created or <see cref="Reset"/> must be released before they can contribute:
/// a new attempt only starts once no button is held at all.</item>
/// <item>After firing, the matcher resets itself (the buttons still held must be released before contributing again).</item>
/// <item>A trigger with no Steps, or with an empty Chord, never fires.</item>
/// </list>
/// Time is passed in as an absolute, monotonic <c>now</c> because the rules compare moments.
/// The Steps and timing of <see cref="Trigger"/> are captured when the matcher is created; create a new matcher after
/// editing the trigger. Not thread-safe: call from a single thread.
/// </remarks>
public sealed class TriggerMatcher
{
    private readonly Chord[] _steps;
    private readonly TimeSpan _stepGap;
    private readonly bool _finalHoldEnabled;
    private readonly TimeSpan _finalHoldDuration;
    private readonly bool _canFire;

    private TriggerMatcherPhase _phase;
    private int _stepIndex;
    private TimeSpan? _previousStepReleasedAt;
    private TimeSpan _finalChordHeldSince;

    public TriggerMatcher(ModeTrigger trigger, ControllerButton heldAtStart = ControllerButton.None)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        Trigger = trigger;
        _steps = [.. trigger.Steps];
        _stepGap = trigger.StepGap;
        _finalHoldEnabled = trigger.FinalHoldEnabled;
        _finalHoldDuration = trigger.FinalHoldDuration;
        // An empty Chord would "complete" whenever nothing is held, firing on idle input.
        _canFire = _steps.Length > 0 && _steps.All(step => !step.IsEmpty);

        Reset(heldAtStart);
    }

    public ModeTrigger Trigger { get; }

    private ControllerButton ExpectedChord => _steps[_stepIndex].Buttons;

    private bool IsLastStep => _stepIndex == _steps.Length - 1;

    /// <summary>Feeds the currently held buttons at time <paramref name="now"/>. Returns <c>true</c> when the trigger fires.</summary>
    public bool Update(ControllerButton held, TimeSpan now)
    {
        if (!_canFire)
        {
            return false;
        }

        return _phase switch
        {
            TriggerMatcherPhase.WaitingForRelease => UpdateWaitingForRelease(held),
            TriggerMatcherPhase.AwaitingStep => UpdateAwaitingStep(held, now),
            TriggerMatcherPhase.ReleasingStep => UpdateReleasingStep(held, now),
            TriggerMatcherPhase.HoldingFinalChord => UpdateHoldingFinalChord(held, now),
            _ => throw new UnreachableException($"Unknown phase {_phase}."),
        };
    }

    /// <summary>Abandons any in-progress attempt. <paramref name="held"/> must be released before contributing.</summary>
    public void Reset(ControllerButton held)
    {
        if (held == ControllerButton.None)
        {
            StartAttempt();
        }
        else
        {
            _phase = TriggerMatcherPhase.WaitingForRelease;
        }
    }

    private void StartAttempt()
    {
        _phase = TriggerMatcherPhase.AwaitingStep;
        _stepIndex = 0;
        _previousStepReleasedAt = null;
    }

    private bool UpdateWaitingForRelease(ControllerButton held)
    {
        if (held == ControllerButton.None)
        {
            StartAttempt();
        }

        return false;
    }

    private bool UpdateAwaitingStep(ControllerButton held, TimeSpan now)
    {
        if (HasStepGapExpired(now) || HoldsButtonOutside(held, ExpectedChord))
        {
            Reset(held);
            return false;
        }

        if (held != ExpectedChord)
        {
            return false;
        }

        return CompleteStep(held, now);
    }

    private bool CompleteStep(ControllerButton held, TimeSpan now)
    {
        if (!IsLastStep)
        {
            _phase = TriggerMatcherPhase.ReleasingStep;
            return false;
        }

        if (!_finalHoldEnabled)
        {
            return Fire(held);
        }

        _phase = TriggerMatcherPhase.HoldingFinalChord;
        _finalChordHeldSince = now;
        return UpdateHoldingFinalChord(held, now);
    }

    private bool UpdateReleasingStep(ControllerButton held, TimeSpan now)
    {
        if (HoldsButtonOutside(held, ExpectedChord))
        {
            Reset(held);
            return false;
        }

        if (held == ControllerButton.None)
        {
            _stepIndex++;
            _previousStepReleasedAt = now;
            _phase = TriggerMatcherPhase.AwaitingStep;
        }

        return false;
    }

    private bool UpdateHoldingFinalChord(ControllerButton held, TimeSpan now)
    {
        if (held != ExpectedChord)
        {
            Reset(held);
            return false;
        }

        if (now - _finalChordHeldSince < _finalHoldDuration)
        {
            return false;
        }

        return Fire(held);
    }

    private bool Fire(ControllerButton held)
    {
        Reset(held);
        return true;
    }

    private bool HasStepGapExpired(TimeSpan now) =>
        _previousStepReleasedAt is { } releasedAt && now - releasedAt > _stepGap;

    private static bool HoldsButtonOutside(ControllerButton held, ControllerButton chord) =>
        (held & ~chord) != ControllerButton.None;
}
