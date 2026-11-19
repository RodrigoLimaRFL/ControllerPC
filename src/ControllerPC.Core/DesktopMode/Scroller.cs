using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Scrolls at a speed proportional to a shaped stick deflection: stick up scrolls up, stick right scrolls right.
/// Sub-notch remainders carry over between updates.
/// </summary>
internal sealed class Scroller
{
    /// <summary>Wheel delta of one notch, as defined by Windows (WHEEL_DELTA).</summary>
    internal const int WheelDeltaPerNotch = 120;

    private readonly IInputSink _sink;
    private readonly WholeUnitAccumulator _vertical = new();
    private readonly WholeUnitAccumulator _horizontal = new();

    public Scroller(IInputSink sink)
    {
        _sink = sink;
    }

    /// <param name="deflection">Shaped deflection, as returned by <see cref="StickCurve.Shape"/>.</param>
    /// <param name="scrollSpeed">Wheel notches per second at full deflection.</param>
    /// <param name="elapsed">Time since the previous update.</param>
    public void Scroll(StickPosition deflection, double scrollSpeed, TimeSpan elapsed)
    {
        var wheelDelta = scrollSpeed * WheelDeltaPerNotch * elapsed.TotalSeconds;
        var vertical = _vertical.Add(deflection.Y * wheelDelta);
        var horizontal = _horizontal.Add(deflection.X * wheelDelta);
        if (vertical != 0 || horizontal != 0)
        {
            _sink.Scroll(vertical, horizontal);
        }
    }

    /// <summary>Discards the sub-notch remainders.</summary>
    public void Reset()
    {
        _vertical.Reset();
        _horizontal.Reset();
    }
}
