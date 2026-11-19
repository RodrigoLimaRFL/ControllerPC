namespace ControllerPC.Windows.Diagnostics;

/// <summary>
/// Remembers the last status reported by each of a fixed number of sources and tells whether a new report
/// differs from it, so a failure repeated on every poll is logged once instead of hundreds of times per second.
/// </summary>
/// <remarks>Thread-safe: reports may come from several threads.</remarks>
internal sealed class StatusChangeFilter
{
    private readonly long[] _lastStatus;

    /// <param name="sourceCount">Number of independent sources (e.g. controller slots).</param>
    /// <param name="initialStatus">Status every source is assumed to start in, normally "success".</param>
    public StatusChangeFilter(int sourceCount, uint initialStatus)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceCount);
        _lastStatus = Enumerable.Repeat((long)initialStatus, sourceCount).ToArray();
    }

    /// <summary>Records <paramref name="status"/> for <paramref name="source"/>; true when it differs from the previous one.</summary>
    public bool Changed(int source, uint status) =>
        Interlocked.Exchange(ref _lastStatus[source], status) != status;
}
