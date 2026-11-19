using ControllerPC.Core.Controller;

namespace ControllerPC.App.Tests.Fakes;

/// <summary>Ignores rumble.</summary>
internal sealed class SilentRumble : IRumble
{
    public void Play(int slot, RumblePattern pattern)
    {
    }
}
