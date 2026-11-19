using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using static ControllerPC.Core.Tests.DesktopMode.ModeControllerHarness;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class ModeControllerProbingTests
{
    private const int DisconnectedSlot = 1;

    private static readonly TimeSpan ProbeInterval = SlotProber.DisconnectedProbeInterval;

    private readonly ModeControllerHarness _harness = new();

    [Fact]
    public void Tick_ConnectedSlot_IsReadEveryTick()
    {
        _harness.Start();
        var readsBefore = _harness.Source.ReadCount(0);

        _harness.Tick();
        _harness.Tick();
        _harness.Tick();

        Assert.Equal(readsBefore + 3, _harness.Source.ReadCount(0));
    }

    [Fact]
    public void Tick_DisconnectedSlot_IsReadOnFirstTick()
    {
        _harness.Start();

        Assert.Equal(1, _harness.Source.ReadCount(DisconnectedSlot));
    }

    [Fact]
    public void Tick_DisconnectedSlot_IsNotReadAgainWithinProbeInterval()
    {
        _harness.Start();

        _harness.TickFor(ProbeInterval - Poll);

        Assert.Equal(1, _harness.Source.ReadCount(DisconnectedSlot));
    }

    [Fact]
    public void Tick_DisconnectedSlot_IsProbedOncePerProbeInterval()
    {
        _harness.Start();

        _harness.TickFor(ProbeInterval * 3);

        Assert.Equal(4, _harness.Source.ReadCount(DisconnectedSlot));
    }

    [Fact]
    public void Tick_SlotConnectsBetweenProbes_IsReadEveryTickOnceFound()
    {
        _harness.Start();
        _harness.Source.Connect(DisconnectedSlot);
        _harness.TickFor(ProbeInterval - Poll);
        Assert.Equal(1, _harness.Source.ReadCount(DisconnectedSlot));

        _harness.Tick();
        _harness.Tick();
        _harness.Tick();

        Assert.Equal(4, _harness.Source.ReadCount(DisconnectedSlot));
    }

    [Fact]
    public void Tick_SlotConnectsWithEntryChordHeld_DoesNotEnterUntilReleased()
    {
        _harness.Start();
        _harness.Source.Set(DisconnectedSlot, EntryChord);

        _harness.TickFor(ProbeInterval * 2);
        Assert.False(_harness.Controller.IsDesktopModeActive);

        _harness.Hold(DisconnectedSlot, ControllerButton.None);
        _harness.Hold(DisconnectedSlot, EntryChord);
        Assert.Equal(DisconnectedSlot, _harness.Controller.DrivingSlot);
    }

    [Fact]
    public void Tick_ConnectedSlotDisconnects_IsThenProbedOncePerProbeInterval()
    {
        _harness.Start();
        _harness.Source.Disconnect(0);
        _harness.Tick();
        var readsAfterDisconnect = _harness.Source.ReadCount(0);

        _harness.TickFor(ProbeInterval - Poll);

        Assert.Equal(readsAfterDisconnect, _harness.Source.ReadCount(0));
    }

    [Fact]
    public void Tick_DrivingControllerDisconnects_IsNotReadAgainWithinProbeInterval()
    {
        _harness.Start();
        _harness.EnterWithBaseTrigger();
        _harness.Source.Disconnect(0);
        _harness.Tick();
        var readsAfterDisconnect = _harness.Source.ReadCount(0);

        _harness.TickFor(ProbeInterval - Poll);

        Assert.Equal(readsAfterDisconnect, _harness.Source.ReadCount(0));
    }
}
