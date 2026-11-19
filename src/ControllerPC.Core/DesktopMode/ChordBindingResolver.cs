using System.Numerics;
using ControllerPC.Core.Controller;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Decides which Chord Bindings are active: one activates when a press completes its whole Chord, the biggest Chord
/// wins over its strict subsets, and it stays active until any of its buttons is released.
/// </summary>
/// <remarks>
/// Chord Bindings with fewer than two buttons are ignored, and of two with the same Chord only the first counts:
/// settings validation rejects both, so this only keeps translation well-defined.
/// </remarks>
internal sealed class ChordBindingResolver
{
    private const int MinimumChordButtons = 2;

    private readonly List<ChordBinding> _chordBindings = [];

    // In activation order.
    private readonly List<ChordBinding> _active = [];

    /// <summary>Every button used in a Chord Binding: these wait for the Chord Wait before their own Binding.</summary>
    public ControllerButton ChordButtons { get; private set; }

    /// <summary>Starts over with copies of <paramref name="chordBindings"/>, so later edits do not affect this session.</summary>
    public void Begin(IEnumerable<ChordBinding> chordBindings)
    {
        Clear();
        foreach (var chordBinding in chordBindings)
        {
            var chord = chordBinding.Chord.Buttons;
            if (ButtonCount(chord) >= MinimumChordButtons && _chordBindings.All(known => known.Chord != chordBinding.Chord))
            {
                _chordBindings.Add(new ChordBinding { Chord = chordBinding.Chord, Action = chordBinding.Action });
                ChordButtons |= chord;
            }
        }
    }

    /// <summary>Deactivates and returns the active Chord Bindings that use any of <paramref name="released"/>.</summary>
    public IReadOnlyList<ChordBinding> Release(ControllerButton released)
    {
        var deactivated = _active.Where(active => Uses(active, released)).ToList();
        _active.RemoveAll(deactivated.Contains);
        return deactivated;
    }

    /// <summary>
    /// Activates the Chord Bindings whose Chord is now fully <paramref name="held"/> thanks to a button in
    /// <paramref name="pressed"/>, biggest first; one that is a strict subset of an active Chord Binding stays inactive.
    /// </summary>
    public IReadOnlyList<ChordActivation> Activate(ControllerButton held, ControllerButton pressed)
    {
        var completed = _chordBindings
            .Where(candidate => !_active.Contains(candidate) && Uses(candidate, pressed) && IsSubset(candidate.Chord.Buttons, held))
            .OrderByDescending(candidate => ButtonCount(candidate.Chord.Buttons))
            .ToList();

        var activations = new List<ChordActivation>();
        foreach (var candidate in completed)
        {
            if (_active.Any(active => IsStrictSubset(candidate, active)))
            {
                continue;
            }

            var displaced = _active.Where(active => IsStrictSubset(active, candidate)).ToList();
            _active.RemoveAll(displaced.Contains);
            _active.Add(candidate);
            activations.Add(new ChordActivation(candidate, displaced));
        }

        return activations;
    }

    /// <summary>Forgets every Chord Binding (until the next <see cref="Begin"/>).</summary>
    public void Clear()
    {
        _chordBindings.Clear();
        _active.Clear();
        ChordButtons = ControllerButton.None;
    }

    private static bool Uses(ChordBinding chordBinding, ControllerButton buttons) =>
        (chordBinding.Chord.Buttons & buttons) != ControllerButton.None;

    private static bool IsSubset(ControllerButton buttons, ControllerButton of) => (buttons & ~of) == ControllerButton.None;

    private static bool IsStrictSubset(ChordBinding smaller, ChordBinding larger) =>
        smaller.Chord != larger.Chord && IsSubset(smaller.Chord.Buttons, larger.Chord.Buttons);

    private static int ButtonCount(ControllerButton buttons) => BitOperations.PopCount((uint)buttons);
}
