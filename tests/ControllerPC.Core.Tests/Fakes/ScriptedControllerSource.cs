using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Tests.Fakes;

/// <summary>An <see cref="IControllerSource"/> whose per-slot states are set by the test; counts reads per slot.</summary>
public sealed class ScriptedControllerSource : IControllerSource
{
    private readonly ControllerState?[] _states;
    private readonly int[] _readCounts;

    /// <summary>Creates a source with <paramref name="slotCount"/> slots, all disconnected.</summary>
    public ScriptedControllerSource(int slotCount)
    {
        SlotCount = slotCount;
        _states = new ControllerState?[slotCount];
        _readCounts = new int[slotCount];
    }

    public int SlotCount { get; }

    public ControllerState? Read(int slot)
    {
        _readCounts[slot]++;
        return _states[slot];
    }

    /// <summary>Connects <paramref name="slot"/> (if needed) with <paramref name="buttons"/> held and both sticks centered.</summary>
    public void Set(int slot, ControllerButton buttons) =>
        _states[slot] = new ControllerState(buttons, StickPosition.Center, StickPosition.Center);

    /// <summary>Connects <paramref name="slot"/> (if needed) with exactly <paramref name="state"/>.</summary>
    public void Set(int slot, ControllerState state) => _states[slot] = state;

    public void Connect(int slot) => Set(slot, ControllerButton.None);

    public void Disconnect(int slot) => _states[slot] = null;

    public int ReadCount(int slot) => _readCounts[slot];
}
