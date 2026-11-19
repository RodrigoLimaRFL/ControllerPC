using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Tests.Fakes;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Tests.DesktopMode;

/// <summary>
/// A <see cref="ModeController"/> over scripted fakes and a test clock. Settings: Base Entry Trigger
/// <see cref="EntryChord"/>, Base Exit Trigger <see cref="ExitChord"/>, Profiles <see cref="Main"/> (active) and
/// <see cref="Second"/>; slot 0 is connected.
/// </summary>
internal sealed class ModeControllerHarness
{
    public const int SlotCount = 4;
    public const ControllerButton EntryChord = ControllerButton.LeftShoulder | ControllerButton.RightShoulder;
    public const ControllerButton ExitChord = ControllerButton.Back | ControllerButton.Start;

    public static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(10);

    private ModeController? _controller;

    public ModeControllerHarness()
    {
        Main = new Profile { Name = "Main" };
        Second = new Profile { Name = "Second" };
        Settings = new AppSettings
        {
            BaseEntryTrigger = Trigger(EntryChord),
            BaseExitTrigger = Trigger(ExitChord),
            Profiles = [Main, Second],
            ActiveProfileId = Main.Id,
        };
        Source.Connect(0);
    }

    public ScriptedControllerSource Source { get; } = new(SlotCount);

    public RecordingRumble Rumble { get; } = new();

    public RecordingInputSink Sink { get; } = new();

    public AppSettings Settings { get; }

    public Profile Main { get; }

    public Profile Second { get; }

    /// <summary>The test clock: advanced by <see cref="Poll"/> on every <see cref="Tick"/>.</summary>
    public TimeSpan Now { get; private set; } = TimeSpan.FromSeconds(100);

    public int StateChangedCount { get; private set; }

    /// <summary>The controller, created on first use from the settings as configured by then.</summary>
    public ModeController Controller
    {
        get
        {
            if (_controller is null)
            {
                _controller = new ModeController(Source, Rumble, Sink, Settings);
                _controller.StateChanged += (_, _) => StateChangedCount++;
            }

            return _controller;
        }
    }

    public static ModeTrigger Trigger(params ControllerButton[] chords) =>
        new() { Steps = [.. chords.Select(buttons => new Chord(buttons))] };

    /// <summary>Creates the controller and ticks once, so the connected slots are seen with nothing held.</summary>
    public ModeControllerHarness Start()
    {
        Tick();
        return this;
    }

    public void Tick()
    {
        Now += Poll;
        Controller.Tick(Now);
    }

    /// <summary>Lets <paramref name="duration"/> pass, ticking every <see cref="Poll"/>.</summary>
    public void TickFor(TimeSpan duration)
    {
        var end = Now + duration;
        while (Now < end)
        {
            Tick();
        }
    }

    /// <summary>Holds exactly <paramref name="buttons"/> on <paramref name="slot"/> and ticks.</summary>
    public void Hold(int slot, ControllerButton buttons)
    {
        Source.Set(slot, buttons);
        Tick();
    }

    /// <summary>Holds <paramref name="buttons"/> on <paramref name="slot"/> for one Tick, then releases them for one.</summary>
    public void Tap(int slot, ControllerButton buttons)
    {
        Hold(slot, buttons);
        Hold(slot, ControllerButton.None);
    }

    /// <summary>Enters Desktop Mode by tapping the Base Entry Trigger on <paramref name="slot"/>.</summary>
    public void EnterWithBaseTrigger(int slot = 0) => Tap(slot, EntryChord);

    public void ClearRecordings()
    {
        Rumble.Played.Clear();
        Sink.ButtonEvents.Clear();
        Sink.KeyCalls.Clear();
        Sink.CursorMoves.Clear();
        StateChangedCount = 0;
    }
}
