namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Accumulates fractional amounts and hands out whole units, carrying the remainder over so that slow movement
/// (less than one pixel or wheel unit per update) still adds up.
/// </summary>
internal sealed class WholeUnitAccumulator
{
    private double _remainder;

    /// <summary>Adds <paramref name="amount"/> and returns the whole units now available, rounded toward zero.</summary>
    public int Add(double amount)
    {
        var total = _remainder + amount;
        var whole = Math.Truncate(total);
        _remainder = total - whole;
        return (int)whole;
    }

    public void Reset() => _remainder = 0;
}
