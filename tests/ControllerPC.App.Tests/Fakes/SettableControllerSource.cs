using ControllerPC.Core.Controller;

namespace ControllerPC.App.Tests.Fakes;

/// <summary>A controller source whose slots the test sets while the host's poll thread reads them.</summary>
internal sealed class SettableControllerSource : IControllerSource
{
    private const int Slots = 4;
    private readonly ControllerState?[] _states = new ControllerState?[Slots];
    private readonly object _gate = new();

    public int SlotCount => Slots;

    public ControllerState? Read(int slot)
    {
        lock (_gate)
        {
            return _states[slot];
        }
    }

    public void Connect(int slot, ControllerButton held = ControllerButton.None)
    {
        lock (_gate)
        {
            _states[slot] = new ControllerState(held, StickPosition.Center, StickPosition.Center);
        }
    }
}
