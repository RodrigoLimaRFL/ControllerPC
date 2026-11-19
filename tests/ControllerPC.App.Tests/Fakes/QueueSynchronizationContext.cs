using System.Collections.Concurrent;

namespace ControllerPC.App.Tests.Fakes;

/// <summary>
/// Stands in for the UI thread: collects posted callbacks and runs them on the test thread when asked.
/// </summary>
internal sealed class QueueSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _posted = new();

    public override void Post(SendOrPostCallback d, object? state) => _posted.Add((d, state));

    /// <summary>Runs posted callbacks until <paramref name="condition"/> holds; false if it did not within <paramref name="timeout"/>.</summary>
    public bool RunUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero || !_posted.TryTake(out var posted, remaining))
            {
                return false;
            }

            posted.Callback(posted.State);
        }

        return true;
    }

    public void Dispose() => _posted.Dispose();
}
