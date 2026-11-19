using System.Collections.Concurrent;

namespace ControllerPC.App.Hosting;

/// <summary>
/// Commands posted from any thread and run, in the order posted, on the one thread that owns their target.
/// </summary>
/// <typeparam name="TArgument">What the owning thread hands every command when it runs (e.g. the current time).</typeparam>
internal sealed class CommandQueue<TArgument>
{
    private readonly ConcurrentQueue<Action<TArgument>> _commands = new();

    /// <summary>Posts <paramref name="command"/>; safe to call from any thread.</summary>
    public void Enqueue(Action<TArgument> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands.Enqueue(command);
    }

    /// <summary>Runs every command posted so far, oldest first. Call only from the owning thread.</summary>
    public void RunPending(TArgument argument)
    {
        while (_commands.TryDequeue(out var command))
        {
            command(argument);
        }
    }
}
