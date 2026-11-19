using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Profiles;

/// <summary>TEMP stub of the agreed contract until feature/chord-bindings-core is merged.</summary>
public sealed class ChordBinding
{
    public Chord Chord { get; set; }

    public ButtonAction Action { get; set; } = NoAction.Instance;
}
