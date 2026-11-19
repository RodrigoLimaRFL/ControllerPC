namespace ControllerPC.Core.Triggers;

/// <summary>Where a <see cref="TriggerMatcher"/> is within one attempt at its Mode Trigger.</summary>
internal enum TriggerMatcherPhase
{
    /// <summary>No attempt is possible until every button is released (after a reset, a fire, or at start).</summary>
    WaitingForRelease,

    /// <summary>Waiting for the current Step's Chord to be held exactly; a strict subset is a Chord being built up.</summary>
    AwaitingStep,

    /// <summary>A Step that is not the last one was completed; waiting for all of its buttons to be released.</summary>
    ReleasingStep,

    /// <summary>The last Chord is held exactly and the Final Hold duration is running.</summary>
    HoldingFinalChord,
}
