using ControllerPC.Core.Controller;

namespace ControllerPC.Core.DesktopMode;

/// <summary>The buttons that went down and came up between two updates.</summary>
internal readonly record struct ButtonEdges(ControllerButton Pressed, ControllerButton Released);
