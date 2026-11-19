using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>A Nudge whose button is held: its direction, step in pixels and repeat timing.</summary>
internal sealed record HeldNudge(NudgeDirection Direction, int Step, NudgeRepeatTimer Timer);
