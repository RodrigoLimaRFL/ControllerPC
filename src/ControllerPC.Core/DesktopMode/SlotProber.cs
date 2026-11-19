using ControllerPC.Core.Controller;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Reads controller slots, re-probing a slot found disconnected at most once per
/// <see cref="DisconnectedProbeInterval"/>: reading an empty XInput slot is expensive, so Microsoft advises
/// against polling disconnected slots every frame. Connected slots are read on every call.
/// </summary>
internal sealed class SlotProber
{
    internal static readonly TimeSpan DisconnectedProbeInterval = TimeSpan.FromSeconds(1);

    private readonly IControllerSource _source;
    private readonly bool[] _connected;
    private readonly TimeSpan?[] _foundDisconnectedAt;

    public SlotProber(IControllerSource source)
    {
        _source = source;
        SlotCount = source.SlotCount;
        _connected = new bool[SlotCount];
        _foundDisconnectedAt = new TimeSpan?[SlotCount];
    }

    public int SlotCount { get; }

    /// <summary>Whether <paramref name="slot"/> was connected when last read.</summary>
    public bool IsConnected(int slot) => _connected[slot];

    /// <summary>
    /// Reads <paramref name="slot"/>; <c>null</c> when it is disconnected, or was found disconnected less than
    /// <see cref="DisconnectedProbeInterval"/> before <paramref name="now"/> (then it is not read at all).
    /// </summary>
    public ControllerState? Read(int slot, TimeSpan now)
    {
        if (_foundDisconnectedAt[slot] is { } foundAt && now - foundAt < DisconnectedProbeInterval)
        {
            return null;
        }

        return Probe(slot, now);
    }

    /// <summary>
    /// Reads <paramref name="slot"/> even if it was found disconnected recently. For rare, user-initiated actions
    /// only, where finding a just-connected controller matters more than the cost of the read.
    /// </summary>
    public ControllerState? Probe(int slot, TimeSpan now)
    {
        var state = _source.Read(slot);
        _connected[slot] = state is not null;
        _foundDisconnectedAt[slot] = state is null ? now : null;
        return state;
    }
}
