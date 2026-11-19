using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>A Chord Binding that just became active, and the active Chord Bindings it replaces (strict subsets of it).</summary>
internal sealed record ChordActivation(ChordBinding Activated, IReadOnlyList<ChordBinding> Displaced);
