using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class StickCurveTests
{
    private const double Deadzone = 0.2;
    private const int Precision = 12;

    [Fact]
    public void Shape_MagnitudeAtDeadzone_ReturnsCenter()
    {
        var shaped = StickCurve.Shape(new StickPosition(0, Deadzone), Deadzone, accelerationEnabled: false);

        Assert.Equal(StickPosition.Center, shaped);
    }

    [Fact]
    public void Shape_EachAxisInsideDeadzoneButMagnitudeOutside_IsNotIgnored()
    {
        var shaped = StickCurve.Shape(new StickPosition(0.18, 0.18), Deadzone, accelerationEnabled: false);

        Assert.True(shaped.X > 0);
        Assert.True(shaped.Y > 0);
    }

    [Fact]
    public void Shape_JustOutsideDeadzone_StartsNearZero()
    {
        var shaped = StickCurve.Shape(new StickPosition(Deadzone + 0.008, 0), Deadzone, accelerationEnabled: false);

        Assert.Equal(0.01, shaped.X, Precision);
    }

    [Fact]
    public void Shape_FullDeflection_ReturnsMagnitudeOne()
    {
        var shaped = StickCurve.Shape(new StickPosition(-1, 0), Deadzone, accelerationEnabled: true);

        Assert.Equal(new StickPosition(-1, 0), shaped);
    }

    [Fact]
    public void Shape_MagnitudeAboveOne_IsClampedToOne()
    {
        var shaped = StickCurve.Shape(new StickPosition(1, 1), Deadzone, accelerationEnabled: false);

        Assert.Equal(Math.Sqrt(0.5), shaped.X, Precision);
        Assert.Equal(Math.Sqrt(0.5), shaped.Y, Precision);
    }

    [Fact]
    public void Shape_PreservesDirection()
    {
        var shaped = StickCurve.Shape(new StickPosition(0.3, -0.4), Deadzone, accelerationEnabled: true);

        Assert.Equal(-0.4 / 0.3, shaped.Y / shaped.X, Precision);
    }

    [Fact]
    public void Shape_AccelerationEnabled_RaisesRescaledMagnitudeToAccelerationExponent()
    {
        // Magnitude 0.6 rescales to 0.5 with a 0.2 deadzone.
        var shaped = StickCurve.Shape(new StickPosition(0, 0.6), Deadzone, accelerationEnabled: true);

        Assert.Equal(Math.Pow(0.5, StickCurve.AccelerationExponent), shaped.Y, Precision);
    }
}
