using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.Core.DesktopMode;

/// <summary>
/// Produces Nudges for held Nudge Bindings: one step on press, then accelerating repeats while held
/// (see <see cref="NudgeRepeatTimer"/>). Each held button repeats independently.
/// </summary>
internal sealed class NudgeRepeater
{
    private readonly IInputSink _sink;
    private readonly Dictionary<ControllerButton, HeldNudge> _held = [];

    public NudgeRepeater(IInputSink sink)
    {
        _sink = sink;
    }

    /// <summary>Nudges once by <paramref name="step"/> pixels and keeps repeating while <paramref name="button"/> is held.</summary>
    public void Press(ControllerButton button, NudgeDirection direction, int step)
    {
        var nudge = new HeldNudge(direction, step, new NudgeRepeatTimer());
        _held[button] = nudge;
        MoveBy(nudge, 1);
    }

    public void Release(ControllerButton button) => _held.Remove(button);

    /// <summary>Performs the repeats that fell due during <paramref name="elapsed"/>.</summary>
    public void Advance(TimeSpan elapsed)
    {
        foreach (var nudge in _held.Values)
        {
            var repeats = nudge.Timer.Advance(elapsed);
            if (repeats > 0)
            {
                MoveBy(nudge, repeats);
            }
        }
    }

    /// <summary>Stops every repeat.</summary>
    public void Clear() => _held.Clear();

    private void MoveBy(HeldNudge nudge, int count)
    {
        var pixels = nudge.Step * count;
        switch (nudge.Direction)
        {
            case NudgeDirection.Up:
                _sink.MoveCursor(0, -pixels);
                break;
            case NudgeDirection.Down:
                _sink.MoveCursor(0, pixels);
                break;
            case NudgeDirection.Left:
                _sink.MoveCursor(-pixels, 0);
                break;
            case NudgeDirection.Right:
                _sink.MoveCursor(pixels, 0);
                break;
        }
    }
}
