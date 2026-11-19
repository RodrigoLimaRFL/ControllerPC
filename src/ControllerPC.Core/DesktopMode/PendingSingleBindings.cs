using ControllerPC.Core.Controller;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// The held buttons used in Chord Bindings whose own Binding waits for the Chord Wait, so a Chord can still complete,
/// and for how long each has been held.
/// </summary>
internal sealed class PendingSingleBindings
{
    private readonly Dictionary<ControllerButton, TimeSpan> _heldFor = [];

    /// <summary>Starts waiting for <paramref name="button"/>, which was just pressed.</summary>
    public void Add(ControllerButton button) => _heldFor[button] = TimeSpan.Zero;

    /// <summary>Stops waiting for <paramref name="button"/>; <c>false</c> when it was not waiting.</summary>
    public bool Remove(ControllerButton button) => _heldFor.Remove(button);

    public void Advance(TimeSpan elapsed)
    {
        foreach (var button in ControllerButtons.All)
        {
            if (_heldFor.TryGetValue(button, out var heldFor))
            {
                _heldFor[button] = heldFor + elapsed;
            }
        }
    }

    /// <summary>Stops waiting for, and returns in <see cref="ControllerButtons.All"/> order, the buttons held for at least <paramref name="chordWait"/>.</summary>
    public IReadOnlyList<ControllerButton> TakeDue(TimeSpan chordWait)
    {
        var due = ControllerButtons.All
            .Where(button => _heldFor.TryGetValue(button, out var heldFor) && heldFor >= chordWait)
            .ToList();
        foreach (var button in due)
        {
            _heldFor.Remove(button);
        }

        return due;
    }

    public void Clear() => _heldFor.Clear();
}
