using System.Diagnostics;
using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>
/// Records Chords from the first controller on which a button is pressed, showing them as they are recorded: every
/// group of buttons held between moments of nothing held becomes one Chord (see <see cref="TriggerRecorder"/>).
/// Records the Steps of a Mode Trigger (<see cref="ForModeTrigger"/>) or a single Chord (<see cref="ForChord"/>).
/// Finishes on "Stop", once the single Chord is recorded, or by itself once nothing has been held for
/// <see cref="AutoFinishIdle"/> after a Chord.
/// </summary>
internal sealed class RecordChordsDialog : ClassicDialog
{
    private const int PollIntervalMilliseconds = 10;
    private const string WaitingForControllerText = "Waiting for a controller...";
    private const int ClientWidth = 360;
    private const int ButtonsY = 214;
    private const int ContentWidth = ClientWidth - (2 * DialogMargin);
    private static readonly TimeSpan AutoFinishIdle = TimeSpan.FromSeconds(2);

    private readonly IControllerSource _controllerSource;
    private readonly TriggerRecorder _recorder = new();
    private readonly Stopwatch _clock = new();
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = PollIntervalMilliseconds };
    private readonly Label _controllerLabel = ClassicControls.Label(string.Empty, DialogMargin, 62);
    private readonly Label _heldLabel = ClassicControls.Label(string.Empty, DialogMargin, 84);
    private readonly ListBox _stepsList = new()
    {
        Location = new Point(DialogMargin, 110),
        Size = new Size(ContentWidth, 96),
        IntegralHeight = false,
        TabStop = false,
    };

    // Per connected slot, the buttons held since the controller was first seen (not yet released).
    private readonly Dictionary<int, ControllerButton> _heldBeforePress = [];

    // Finishes as soon as this many Chords are recorded; null for no limit.
    private readonly int? _chordLimit;

    private int? _slot;

    private RecordChordsDialog(IControllerSource controllerSource, string title, string instructionText, int? chordLimit)
        : base(title)
    {
        _controllerSource = controllerSource;
        _chordLimit = chordLimit;

        var instructions = new Label
        {
            Text = instructionText,
            Location = new Point(DialogMargin, DialogMargin),
            Size = new Size(ContentWidth, 45),
        };
        var stopButton = ClassicControls.Button("&Stop", ButtonRowX(ClientWidth, 1), ButtonsY, (_, _) => Finish());
        var cancelButton = ClassicControls.Button("Cancel", ButtonRowX(ClientWidth, 0), ButtonsY, (_, _) => Close());
        AcceptButton = stopButton;
        CancelButton = cancelButton;

        Controls.AddRange([instructions, _controllerLabel, _heldLabel, _stepsList, stopButton, cancelButton]);
        _pollTimer.Tick += (_, _) => Poll();
        ShowStatus(WaitingForControllerText, ControllerButton.None);
        CompleteLayout(new Size(ClientWidth, ButtonsY + ClassicControls.ButtonHeight + DialogMargin));
    }

    /// <summary>The recorded Chords; empty when the dialog was cancelled or nothing was recorded.</summary>
    public List<Chord> RecordedChords { get; private set; } = [];

    /// <summary>Records the Steps of a Mode Trigger, one Chord per Step.</summary>
    public static RecordChordsDialog ForModeTrigger(IControllerSource controllerSource) =>
        new(
            controllerSource,
            "Record Mode Trigger",
            "Press the Chord of each Step on the controller, releasing every button between Steps. "
            + $"Recording stops by itself {AutoFinishIdle.TotalSeconds:0} seconds after the last release.",
            chordLimit: null);

    /// <summary>Records one Chord, finishing as soon as its buttons are released.</summary>
    public static RecordChordsDialog ForChord(IControllerSource controllerSource) =>
        new(
            controllerSource,
            "Record Chord",
            "Hold the buttons of the Chord together on the controller, then release them.",
            chordLimit: 1);

    /// <summary>
    /// Shows the dialog modally and returns the recorded Chords (empty when cancelled). <paramref name="onStarted"/>
    /// runs just before it opens and <paramref name="onFinished"/> always runs after it closes, even if
    /// <paramref name="onStarted"/> throws, so a pause they bracket is always ended.
    /// </summary>
    public List<Chord> Record(IWin32Window? owner, Action onStarted, Action onFinished)
    {
        DialogResult result;
        try
        {
            onStarted();
            result = ShowDialog(owner);
        }
        finally
        {
            onFinished();
        }

        return result == DialogResult.OK ? RecordedChords : [];
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _clock.Start();
        _pollTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _pollTimer.Stop();
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Poll()
    {
        var now = _clock.Elapsed;
        if (_slot is not { } slot)
        {
            WaitForFirstPress(now);
            return;
        }

        // While disconnected nothing is known about the buttons: feeding "nothing held" would end the Step in
        // progress, so recording simply pauses until the controller is back.
        if (_controllerSource.Read(slot) is not { } state)
        {
            ShowStatus($"Controller {slot + 1} disconnected; reconnect it to continue.", ControllerButton.None);
            return;
        }

        _recorder.Update(state.Buttons, now);
        ShowStatus(RecordingText(slot), state.Buttons);
        ShowSteps();

        if (_chordLimit is { } limit && _recorder.Steps.Count >= limit || _recorder.IsIdleFor(AutoFinishIdle, now))
        {
            Finish();
        }
    }

    /// <summary>
    /// Records from the first controller on which a button goes down. Buttons already held when a controller was
    /// first seen do not count until released, so a pad resting with a button held is not picked.
    /// </summary>
    private void WaitForFirstPress(TimeSpan now)
    {
        for (var slot = 0; slot < _controllerSource.SlotCount; slot++)
        {
            if (_controllerSource.Read(slot) is not { } state)
            {
                _heldBeforePress.Remove(slot);
                continue;
            }

            if (!_heldBeforePress.TryGetValue(slot, out var heldBefore))
            {
                _heldBeforePress[slot] = state.Buttons;
                continue;
            }

            var stillHeld = heldBefore & state.Buttons;
            _heldBeforePress[slot] = stillHeld;
            if ((state.Buttons & ~stillHeld) != ControllerButton.None)
            {
                _slot = slot;
                _recorder.Start(stillHeld, now);
                _recorder.Update(state.Buttons, now);
                ShowStatus(RecordingText(slot), state.Buttons);
                return;
            }
        }

        ShowStatus(
            _heldBeforePress.Count == 0 ? WaitingForControllerText : "Press a button on the controller to record from.",
            ControllerButton.None);
    }

    private static string RecordingText(int slot) => $"Recording from controller {slot + 1}.";

    private void ShowStatus(string controllerText, ControllerButton held)
    {
        _controllerLabel.Text = controllerText;
        _heldLabel.Text = $"Holding: {(held == ControllerButton.None ? "nothing" : DisplayNames.ForChord(new Chord(held)))}";
    }

    private void ShowSteps()
    {
        var steps = _recorder.Steps;
        if (steps.Count == _stepsList.Items.Count)
        {
            return;
        }

        _stepsList.Items.Clear();
        for (var i = 0; i < steps.Count; i++)
        {
            _stepsList.Items.Add(DisplayNames.ForStep(i, steps[i]));
        }
    }

    private void Finish()
    {
        _pollTimer.Stop();
        IEnumerable<Chord> recorded = _slot is null ? [] : _recorder.Steps;
        RecordedChords = [.. recorded.Take(_chordLimit ?? int.MaxValue)];
        DialogResult = DialogResult.OK;
    }
}
