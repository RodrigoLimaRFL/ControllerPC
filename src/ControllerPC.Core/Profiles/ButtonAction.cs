using System.Text.Json.Serialization;

namespace ControllerPC.Core.Profiles;

/// <summary>
/// An Action: what a Binding produces while its button is held in Desktop Mode.
/// (Named <c>ButtonAction</c> to avoid clashing with <see cref="System.Action"/>.)
/// </summary>
/// <remarks>
/// Serialized with a <c>"type"</c> discriminator, which must be the first property of each JSON object.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoAction), "none")]
[JsonDerivedType(typeof(MouseButtonAction), "mouse")]
[JsonDerivedType(typeof(KeyCombinationAction), "keys")]
[JsonDerivedType(typeof(NudgeAction), "nudge")]
[JsonDerivedType(typeof(NextProfileAction), "nextProfile")]
[JsonDerivedType(typeof(SwitchToProfileAction), "switchProfile")]
public abstract record ButtonAction;
