namespace ControllerPC.Core.Profiles;

/// <summary>Per-Profile tuning of cursor, Nudge and scroll behavior.</summary>
public sealed class MovementTuning
{
    public const double DefaultDeadzone = 0.2;
    public const double DefaultCursorSpeed = 1200;
    public const int DefaultNudgeStep = 1;
    public const double DefaultScrollSpeed = 10;

    /// <summary>Stick deflection in [0, 1) below which input is ignored.</summary>
    public double Deadzone { get; set; } = DefaultDeadzone;

    /// <summary>Cursor speed at full stick deflection, in pixels per second (&gt; 0).</summary>
    public double CursorSpeed { get; set; } = DefaultCursorSpeed;

    /// <summary>When enabled, cursor speed grows non-linearly with deflection for finer control near the center.</summary>
    public bool AccelerationEnabled { get; set; } = true;

    /// <summary>Pixels moved by a single Nudge (≥ 1).</summary>
    public int NudgeStep { get; set; } = DefaultNudgeStep;

    /// <summary>Scroll speed at full stick deflection, in wheel notches per second (&gt; 0).</summary>
    public double ScrollSpeed { get; set; } = DefaultScrollSpeed;
}
