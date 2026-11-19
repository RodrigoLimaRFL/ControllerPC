using ControllerPC.Core.Controller;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Turns successive held-button sets into press and release edges. Buttons held when <see cref="Begin"/> is called
/// produce no edges until they have been released.
/// </summary>
internal sealed class ButtonEdgeDetector
{
    private ControllerButton _ignored;
    private ControllerButton _previous;

    public void Begin(ControllerButton heldAtStart)
    {
        _ignored = heldAtStart;
        _previous = ControllerButton.None;
    }

    public ButtonEdges Update(ControllerButton held)
    {
        _ignored &= held;
        var counted = held & ~_ignored;
        var edges = new ButtonEdges(Pressed: counted & ~_previous, Released: _previous & ~counted);
        _previous = counted;
        return edges;
    }
}
