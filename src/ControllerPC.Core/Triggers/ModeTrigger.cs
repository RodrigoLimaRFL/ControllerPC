namespace ControllerPC.Core.Triggers;

/// <summary>
/// A Mode Trigger: an ordered list of Steps (each exactly one Chord) that switches Desktop Mode on or off,
/// together with its own timing rules.
/// </summary>
public sealed class ModeTrigger
{
    public static readonly TimeSpan DefaultStepGap = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan DefaultFinalHoldDuration = TimeSpan.FromMilliseconds(500);

    /// <summary>The Steps, in order. Each Step is exactly one Chord.</summary>
    public List<Chord> Steps { get; set; } = [];

    /// <summary>Maximum time between releasing one Step and completing the next before the attempt resets.</summary>
    public TimeSpan StepGap { get; set; } = DefaultStepGap;

    /// <summary>Whether the last Chord must be held for <see cref="FinalHoldDuration"/> before the trigger fires.</summary>
    public bool FinalHoldEnabled { get; set; }

    public TimeSpan FinalHoldDuration { get; set; } = DefaultFinalHoldDuration;
}
