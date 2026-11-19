using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Profiles;

/// <summary>
/// A Chord Binding: an Action performed while every button of a Chord of two or more buttons is held in Desktop Mode.
/// </summary>
public sealed class ChordBinding
{
    public Chord Chord { get; set; }

    public ButtonAction Action { get; set; } = NoAction.Instance;
}
