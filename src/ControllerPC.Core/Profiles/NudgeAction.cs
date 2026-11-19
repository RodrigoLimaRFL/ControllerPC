namespace ControllerPC.Core.Profiles;

/// <summary>Nudges the cursor one step on press, then repeatedly and accelerating while held.</summary>
public sealed record NudgeAction(NudgeDirection Direction) : ButtonAction;
