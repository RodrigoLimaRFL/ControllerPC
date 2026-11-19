using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Triggers;

/// <summary>A Chord: a set of one or more controller buttons held down at the same time.</summary>
public readonly record struct Chord(ControllerButton Buttons)
{
    public bool IsEmpty => Buttons == ControllerButton.None;
}
