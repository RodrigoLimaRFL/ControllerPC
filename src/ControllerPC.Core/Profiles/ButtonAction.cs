using System.Text.Json.Serialization;
using ControllerPC.Core.Output;

namespace ControllerPC.Core.Profiles;

/// <summary>An Action: what a Binding produces while its button is held in Desktop Mode.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoAction), "none")]
[JsonDerivedType(typeof(MouseButtonAction), "mouse")]
[JsonDerivedType(typeof(KeyCombinationAction), "keys")]
[JsonDerivedType(typeof(NudgeAction), "nudge")]
[JsonDerivedType(typeof(NextProfileAction), "nextProfile")]
[JsonDerivedType(typeof(SwitchToProfileAction), "switchProfile")]
public abstract record ButtonAction;

public sealed record NoAction : ButtonAction
{
    public static NoAction Instance { get; } = new();
}

/// <summary>Holds a mouse button for as long as the controller button is held.</summary>
public sealed record MouseButtonAction(MouseButton Button) : ButtonAction;

/// <summary>Holds a Key Combination for as long as the controller button is held.</summary>
public sealed record KeyCombinationAction(KeyCombination Keys) : ButtonAction;

public enum NudgeDirection
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>Nudges the cursor one step on press, then repeatedly and accelerating while held.</summary>
public sealed record NudgeAction(NudgeDirection Direction) : ButtonAction;

/// <summary>An Action that changes the Active Profile when pressed.</summary>
public abstract record ProfileSwitchAction : ButtonAction;

/// <summary>Switches to the next Profile in list order, wrapping around.</summary>
public sealed record NextProfileAction : ProfileSwitchAction;

/// <summary>Switches to a specific Profile; acts as nothing if that Profile no longer exists.</summary>
public sealed record SwitchToProfileAction(Guid ProfileId) : ProfileSwitchAction;
