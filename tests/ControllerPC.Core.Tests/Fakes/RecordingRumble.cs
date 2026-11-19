using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Tests.Fakes;

/// <summary>An <see cref="IRumble"/> that records every pattern played.</summary>
public sealed class RecordingRumble : IRumble
{
    public List<(int Slot, RumblePattern Pattern)> Played { get; } = [];

    public void Play(int slot, RumblePattern pattern) => Played.Add((slot, pattern));
}
