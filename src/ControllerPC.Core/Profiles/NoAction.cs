namespace ControllerPC.Core.Profiles;

/// <summary>The Action of an unbound button: does nothing.</summary>
public sealed record NoAction : ButtonAction
{
    public static NoAction Instance { get; } = new();
}
