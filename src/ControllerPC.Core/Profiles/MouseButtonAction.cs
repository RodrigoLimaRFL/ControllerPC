using ControllerPC.Core.Output;

namespace ControllerPC.Core.Profiles;

/// <summary>Holds a mouse button for as long as the controller button is held.</summary>
public sealed record MouseButtonAction(MouseButton Button) : ButtonAction;
