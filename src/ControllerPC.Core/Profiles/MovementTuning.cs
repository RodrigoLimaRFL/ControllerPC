namespace ControllerPC.Core.Profiles;

/// <summary>Per-Profile tuning of cursor, Nudge and scroll behavior.</summary>
public sealed class MovementTuning
{
    /// <summary>Stick deflection (0..1) below which input is ignored.</summary>
    public double Deadzone { get; set; } = 0.2;

    /// <summary>Cursor speed at full stick deflection, in pixels per second.</summary>
    public double CursorSpeed { get; set; } = 1200;

    /// <summary>When enabled, cursor speed grows non-linearly with deflection for finer control near the center.</summary>
    public bool AccelerationEnabled { get; set; } = true;

    /// <summary>Pixels moved by a single Nudge.</summary>
    public int NudgeStep { get; set; } = 1;

    /// <summary>Scroll speed at full stick deflection, in wheel notches per second.</summary>
    public double ScrollSpeed { get; set; } = 10;
}
