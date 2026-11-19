using ControllerPC.Core.Controller;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// The buttons that went down and came up between two updates, and those now held (both leaving out buttons still
/// held since <see cref="ButtonEdgeDetector.Begin"/>).
/// </summary>
internal readonly record struct ButtonEdges(ControllerButton Pressed, ControllerButton Released, ControllerButton Held);
