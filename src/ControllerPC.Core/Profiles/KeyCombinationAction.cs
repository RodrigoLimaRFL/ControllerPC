using ControllerPC.Core.Output;

namespace ControllerPC.Core.Profiles;

/// <summary>Holds a Key Combination for as long as the controller button is held.</summary>
public sealed record KeyCombinationAction(KeyCombination Keys) : ButtonAction;
