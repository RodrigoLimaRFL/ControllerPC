using ControllerPC.App.Hosting;

namespace ControllerPC.App.Tests.Hosting;

public class CommandQueueTests
{
    [Fact]
    public void RunPending_RunsCommandsInTheOrderTheyWerePosted()
    {
        var queue = new CommandQueue<int>();
        var ran = new List<string>();
        queue.Enqueue(_ => ran.Add("first"));
        queue.Enqueue(_ => ran.Add("second"));

        queue.RunPending(0);

        Assert.Equal(["first", "second"], ran);
    }

    [Fact]
    public void RunPending_PassesTheArgumentToEveryCommand()
    {
        var queue = new CommandQueue<int>();
        var received = new List<int>();
        queue.Enqueue(received.Add);
        queue.Enqueue(received.Add);

        queue.RunPending(42);

        Assert.Equal([42, 42], received);
    }

    [Fact]
    public void RunPending_RunsEachCommandOnlyOnce()
    {
        var queue = new CommandQueue<int>();
        var runs = 0;
        queue.Enqueue(_ => runs++);
        queue.RunPending(0);

        queue.RunPending(0);

        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task RunPending_RunsCommandsPostedFromOtherThreads()
    {
        var queue = new CommandQueue<int>();
        var runs = 0;
        var posters = Enumerable.Range(0, 4).Select(_ => Task.Run(() => queue.Enqueue(_ => runs++)));
        await Task.WhenAll(posters);

        queue.RunPending(0);

        Assert.Equal(4, runs);
    }
}
