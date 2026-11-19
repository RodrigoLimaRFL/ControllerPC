using System.Diagnostics;
using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>
/// Records the Steps of a Mode Trigger from the first connected controller, showing them as they are recorded.
/// Finishes on "Stop", or by itself once nothing has been held for <see cref="AutoFinishIdle"/> after a Step.
/// </summary>
internal sealed class RecordTriggerDialog : ClassicDialog
{
    private const int PollIntervalMilliseconds = 10;
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

    private int? _slot;

    public RecordTriggerDialog(IControllerSource controllerSource)
        : base("Record Mode Trigger")
    {
        _controllerSource = controllerSource;

        var instructions = new Label
        {
            Text = "Press the Chord of each Step on the controller, releasing every button between Steps. "
                + $"Recording stops by itself {AutoFinishIdle.TotalSeconds:0} seconds after the last release.",
            Location = new Point(DialogMargin, DialogMargin),
            Size = new Size(ContentWidth, 45),
        };
        var stopButton = ClassicControls.Button("&Stop", ButtonRowX(ClientWidth, 1), ButtonsY, (_, _) => Finish());
        var cancelButton = ClassicControls.Button("Cancel", ButtonRowX(ClientWidth, 0), ButtonsY, (_, _) => Close());
        AcceptButton = stopButton;
        CancelButton = cancelButton;

        Controls.AddRange([instructions, _controllerLabel, _heldLabel, _stepsList, stopButton, cancelButton]);
        _pollTimer.Tick += (_, _) => Poll();
        ShowStatus("Waiting for a controller...", ControllerButton.None);
        CompleteLayout(new Size(ClientWidth, ButtonsY + ClassicControls.ButtonHeight + DialogMargin));
    }

    /// <summary>The recorded Steps; empty when the dialog was cancelled or nothing was recorded.</summary>
    public List<Chord> RecordedSteps { get; private set; } = [];

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
            StartWithFirstConnectedController(now);
            return;
        }

        var state = _controllerSource.Read(slot);
        var held = state?.Buttons ?? ControllerButton.None;
        _recorder.Update(held, now);
        ShowStatus(state is null ? "Controller disconnected." : $"Recording from controller {slot + 1}.", held);
        ShowSteps();

        if (_recorder.IsIdleFor(AutoFinishIdle, now))
        {
            Finish();
        }
    }

    private void StartWithFirstConnectedController(TimeSpan now)
    {
        for (var slot = 0; slot < _controllerSource.SlotCount; slot++)
        {
            if (_controllerSource.Read(slot) is { } state)
            {
                _slot = slot;
                _recorder.Start(state.Buttons, now);
                ShowStatus($"Recording from controller {slot + 1}.", state.Buttons);
                return;
            }
        }
    }

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
        RecordedSteps = _slot is null ? [] : [.. _recorder.Steps];
        DialogResult = DialogResult.OK;
    }
}
