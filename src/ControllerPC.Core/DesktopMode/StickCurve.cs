using ControllerPC.Core.Controller;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Shapes a raw stick position into a deflection: a radial deadzone on the stick's magnitude, the remaining
/// range rescaled to [0, 1], and an optional acceleration curve. The direction is preserved.
/// </summary>
internal static class StickCurve
{
    /// <summary>
    /// Exponent applied to the rescaled magnitude when acceleration is enabled: squaring keeps full deflection at
    /// full speed while giving finer control near the center.
    /// </summary>
    internal const double AccelerationExponent = 2;

    private const double FullDeflection = 1;

    public static StickPosition Shape(StickPosition raw, double deadzone, bool accelerationEnabled)
    {
        var rawMagnitude = Math.Sqrt((raw.X * raw.X) + (raw.Y * raw.Y));

        // Diagonals of a square-gated stick report magnitudes above 1; treat them as full deflection.
        var magnitude = Math.Min(rawMagnitude, FullDeflection);
        if (magnitude <= deadzone)
        {
            return StickPosition.Center;
        }

        var rescaled = (magnitude - deadzone) / (FullDeflection - deadzone);
        var shaped = accelerationEnabled ? Math.Pow(rescaled, AccelerationExponent) : rescaled;
        return new StickPosition(raw.X / rawMagnitude * shaped, raw.Y / rawMagnitude * shaped);
    }
}
