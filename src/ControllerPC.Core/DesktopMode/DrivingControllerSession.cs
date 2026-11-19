using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.DesktopMode;

/// <summary>The state of one stay in Desktop Mode: the Driving Controller's slot and what watches it.</summary>
internal sealed class DrivingControllerSession
{
    public DrivingControllerSession(int slot, TriggerMatcher exitMatcher)
    {
        Slot = slot;
        ExitMatcher = exitMatcher;
    }

    public int Slot { get; }

    /// <summary>Watches the Effective Exit Trigger of the Active Profile; replaced when that Profile changes.</summary>
    public TriggerMatcher ExitMatcher { get; set; }

    /// <summary>When input was last translated; <c>null</c> until the first translated Tick, which then has no elapsed time.</summary>
    public TimeSpan? LastTranslatedAt { get; set; }
}
