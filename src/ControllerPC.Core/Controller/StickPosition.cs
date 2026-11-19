namespace ControllerPC.Core.Controller;

/// <summary>Position of an analog stick, each axis normalized to [-1, 1]. Positive Y is up (away from the user).</summary>
public readonly record struct StickPosition(double X, double Y)
{
    public static StickPosition Center => new(0, 0);
}
