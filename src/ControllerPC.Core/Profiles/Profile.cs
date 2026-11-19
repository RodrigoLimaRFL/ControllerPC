using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Profiles;

/// <summary>
/// A Profile: a named set of Bindings, Right Stick Role, movement tuning and optional Trigger Overrides.
/// </summary>
public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    /// <summary>The Bindings: one Action per single digital button. Missing buttons act as nothing.</summary>
    public Dictionary<ControllerButton, ButtonAction> Bindings { get; set; } = [];

    public RightStickRole RightStickRole { get; set; } = RightStickRole.Scroll;

    public MovementTuning Tuning { get; set; } = new();

    /// <summary>Trigger Override for entering Desktop Mode; <c>null</c> means the Base Entry Trigger applies.</summary>
    public ModeTrigger? EntryTriggerOverride { get; set; }

    /// <summary>Trigger Override for exiting Desktop Mode; <c>null</c> means the Base Exit Trigger applies.</summary>
    public ModeTrigger? ExitTriggerOverride { get; set; }

    /// <summary>The Action bound to <paramref name="button"/>, or <see cref="NoAction"/>.</summary>
    public ButtonAction GetAction(ControllerButton button) =>
        Bindings.TryGetValue(button, out var action) ? action : NoAction.Instance;
}
