using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Profiles;

/// <summary>
/// A Profile: a named set of Bindings, Chord Bindings, Right Stick Role, movement tuning and optional Trigger Overrides.
/// </summary>
public sealed class Profile
{
    public static readonly TimeSpan DefaultChordWait = TimeSpan.FromMilliseconds(75);

    /// <summary>The longest Chord Wait allowed; longer would make the single Bindings of Chord buttons feel laggy.</summary>
    public static readonly TimeSpan MaxChordWait = TimeSpan.FromMilliseconds(500);

    /// <summary>Stable identity, so Profile switch Actions survive renames. A new Profile gets a fresh id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    /// <summary>
    /// The Bindings: one Action per single digital button (each key is exactly one <see cref="ControllerButton"/> flag).
    /// Missing buttons act as nothing.
    /// </summary>
    public Dictionary<ControllerButton, ButtonAction> Bindings { get; set; } = [];

    /// <summary>The Chord Bindings, each for a Chord of two or more buttons; no two share a Chord.</summary>
    public List<ChordBinding> ChordBindings { get; set; } = [];

    /// <summary>
    /// The Chord Wait: how long a button used in any Chord Binding waits before performing its own Binding, in
    /// [0, <see cref="MaxChordWait"/>]. Zero performs it at once.
    /// </summary>
    public TimeSpan ChordWait { get; set; } = DefaultChordWait;

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
