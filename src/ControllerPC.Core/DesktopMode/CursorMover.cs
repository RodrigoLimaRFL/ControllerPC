using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Moves the cursor at a speed proportional to a shaped stick deflection, flipping stick Y (up-positive)
/// into screen Y (down-positive) and carrying sub-pixel remainders between updates.
/// </summary>
internal sealed class CursorMover
{
    private readonly IInputSink _sink;
    private readonly WholeUnitAccumulator _horizontal = new();
    private readonly WholeUnitAccumulator _vertical = new();

    public CursorMover(IInputSink sink)
    {
        _sink = sink;
    }

    /// <param name="deflection">Shaped deflection, as returned by <see cref="StickCurve.Shape"/>.</param>
    /// <param name="cursorSpeed">Pixels per second at full deflection.</param>
    /// <param name="elapsed">Time since the previous update.</param>
    public void Move(StickPosition deflection, double cursorSpeed, TimeSpan elapsed)
    {
        var pixels = cursorSpeed * elapsed.TotalSeconds;
        var dx = _horizontal.Add(deflection.X * pixels);
        var dy = _vertical.Add(-deflection.Y * pixels);
        if (dx != 0 || dy != 0)
        {
            _sink.MoveCursor(dx, dy);
        }
    }

    /// <summary>Discards the sub-pixel remainders.</summary>
    public void Reset()
    {
        _horizontal.Reset();
        _vertical.Reset();
    }
}
