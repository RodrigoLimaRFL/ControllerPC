using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>
/// Edits one Mode Trigger in place: its Steps (recorded from the controller or ticked by hand), Step Gap and
/// Final Hold.
/// </summary>
internal sealed class ModeTriggerEditor : UserControl
{
    // The Steps list and its buttons span the width of the Chord grid below them.
    private const int ListWidth = (ChordColumns * ChordColumnWidth) + ChordPadding - ClassicControls.ButtonWidth - ButtonGap;
    private const int ButtonGap = 10;
    private const int ListTop = 18;

    // As tall as the column of four buttons beside it.
    private const int ListHeight = (3 * ClassicControls.RowHeight) + ClassicControls.ButtonHeight;
    private const int ButtonX = ListWidth + ButtonGap;
    private const int ChordTop = ListTop + ListHeight + 8;
    private const int ChordColumns = 4;
    private const int ChordColumnWidth = 102;
    private const int ChordRowHeight = 23;
    private const int ChordCellGap = 2;
    private const int ChordPadding = 10;
    private const int ChordBoxTop = 20;
    private const int TimingFieldX = 100;
    private const int TimingFieldWidth = 70;
    private const decimal MinMilliseconds = 50;
    private const decimal MaxMilliseconds = 10000;
    private const decimal MillisecondsIncrement = 50;

    private readonly IControllerSource _controllerSource;
    private readonly ListBox _stepsList = new()
    {
        Location = new Point(0, ListTop),
        Size = new Size(ListWidth, ListHeight),
        IntegralHeight = false,
    };

    private readonly Button _recordButton;
    private readonly Button _addButton;
    private readonly Button _removeButton;
    private readonly Button _clearButton;
    private readonly GroupBox _chordGroup;
    private readonly Dictionary<ControllerButton, CheckBox> _chordBoxes = [];
    private readonly NumericUpDown _stepGapBox;
    private readonly CheckBox _finalHoldBox;
    private readonly NumericUpDown _finalHoldDurationBox;

    private ModeTrigger? _trigger;
    private bool _loading;

    public ModeTriggerEditor(IControllerSource controllerSource)
    {
        _controllerSource = controllerSource;
        AutoScaleMode = AutoScaleMode.Inherit;

        _recordButton = ClassicControls.Button("Record...", ButtonX, ListTop, (_, _) => Record());
        _addButton = ClassicControls.Button("Add Step", ButtonX, ListTop + ClassicControls.RowHeight, (_, _) => AddStep());
        _removeButton = ClassicControls.Button("Remove Step", ButtonX, ListTop + (2 * ClassicControls.RowHeight), (_, _) => RemoveStep());
        _clearButton = ClassicControls.Button("Clear", ButtonX, ListTop + (3 * ClassicControls.RowHeight), (_, _) => ClearSteps());

        _chordGroup = CreateChordGroup();
        var timingTop = _chordGroup.Bottom + 8;
        _stepGapBox = ClassicControls.Number(TimingFieldX, timingTop, TimingFieldWidth, MinMilliseconds, MaxMilliseconds, MillisecondsIncrement);
        _finalHoldBox = ClassicControls.CheckBox("Final Hold:", 0, timingTop + ClassicControls.RowHeight + 1);
        _finalHoldDurationBox = ClassicControls.Number(
            TimingFieldX, timingTop + ClassicControls.RowHeight, TimingFieldWidth, MinMilliseconds, MaxMilliseconds, MillisecondsIncrement);

        Controls.AddRange(
        [
            ClassicControls.Label("Steps:", 0, 0), _stepsList, _recordButton, _addButton, _removeButton, _clearButton,
            _chordGroup,
            ClassicControls.Label("Step Gap:", 0, timingTop), _stepGapBox,
            ClassicControls.Label("ms", TimingFieldX + TimingFieldWidth + 6, timingTop),
            _finalHoldBox, _finalHoldDurationBox,
            ClassicControls.Label("ms", TimingFieldX + TimingFieldWidth + 6, timingTop + ClassicControls.RowHeight),
        ]);
        Size = new Size(ButtonX + ClassicControls.ButtonWidth, timingTop + (2 * ClassicControls.RowHeight));

        _stepsList.SelectedIndexChanged += (_, _) => ShowSelectedChord();
        _stepGapBox.ValueChanged += (_, _) => StoreTiming();
        _finalHoldBox.CheckedChanged += (_, _) => StoreTiming();
        _finalHoldDurationBox.ValueChanged += (_, _) => StoreTiming();

        Edit(null);
    }

    /// <summary>Edits <paramref name="trigger"/> in place; <c>null</c> shows nothing and disables editing.</summary>
    public void Edit(ModeTrigger? trigger)
    {
        _loading = true;
        _trigger = trigger;
        ClassicControls.SetClamped(_stepGapBox, (decimal)(trigger?.StepGap ?? ModeTrigger.DefaultStepGap).TotalMilliseconds);
        _finalHoldBox.Checked = trigger?.FinalHoldEnabled ?? false;
        ClassicControls.SetClamped(
            _finalHoldDurationBox, (decimal)(trigger?.FinalHoldDuration ?? ModeTrigger.DefaultFinalHoldDuration).TotalMilliseconds);
        _loading = false;
        ShowSteps(selectedIndex: 0);
    }

    private GroupBox CreateChordGroup()
    {
        var rows = (ControllerButtons.All.Count + ChordColumns - 1) / ChordColumns;
        var group = ClassicControls.Group(
            "Chord of the selected Step",
            0,
            ChordTop,
            (ChordColumns * ChordColumnWidth) + ChordPadding,
            ChordBoxTop + (rows * ChordRowHeight) + (ChordPadding / 2));

        for (var i = 0; i < ControllerButtons.All.Count; i++)
        {
            var button = ControllerButtons.All[i];
            var box = ClassicControls.CheckBox(
                DisplayNames.ForButton(button),
                ChordPadding + ((i % ChordColumns) * ChordColumnWidth),
                ChordBoxTop + ((i / ChordColumns) * ChordRowHeight));

            // A fixed size keeps the grid regular when it is scaled; auto-sized boxes overlapped their neighbors.
            box.AutoSize = false;
            box.Size = new Size(ChordColumnWidth - ChordCellGap, ChordRowHeight - ChordCellGap);
            box.CheckedChanged += (_, _) => StoreSelectedChord();
            _chordBoxes[button] = box;
            group.Controls.Add(box);
        }

        return group;
    }

    private void ShowSteps(int selectedIndex)
    {
        _loading = true;
        _stepsList.Items.Clear();
        var steps = _trigger?.Steps ?? [];
        for (var i = 0; i < steps.Count; i++)
        {
            _stepsList.Items.Add(DisplayNames.ForStep(i, steps[i]));
        }

        _stepsList.SelectedIndex = steps.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, steps.Count - 1);
        _loading = false;
        ShowSelectedChord();
    }

    private void ShowSelectedChord()
    {
        var editable = _trigger is not null;
        var index = _stepsList.SelectedIndex;
        var chord = editable && index >= 0 ? _trigger!.Steps[index] : default;

        _loading = true;
        foreach (var (button, box) in _chordBoxes)
        {
            box.Checked = chord.Buttons.HasFlag(button);
        }

        _loading = false;

        _stepsList.Enabled = _recordButton.Enabled = _addButton.Enabled = editable;
        _stepGapBox.Enabled = _finalHoldBox.Enabled = editable;
        _finalHoldDurationBox.Enabled = editable && _finalHoldBox.Checked;
        _removeButton.Enabled = editable && index >= 0;
        _clearButton.Enabled = editable && _stepsList.Items.Count > 0;
        _chordGroup.Enabled = editable && index >= 0;
    }

    private void StoreSelectedChord()
    {
        var index = _stepsList.SelectedIndex;
        if (_loading || _trigger is null || index < 0)
        {
            return;
        }

        var buttons = _chordBoxes.Where(pair => pair.Value.Checked).Aggregate(ControllerButton.None, (all, pair) => all | pair.Key);
        _trigger.Steps[index] = new Chord(buttons);
        _loading = true;
        _stepsList.Items[index] = DisplayNames.ForStep(index, _trigger.Steps[index]);
        _stepsList.SelectedIndex = index;
        _loading = false;
    }

    private void StoreTiming()
    {
        if (_loading || _trigger is null)
        {
            return;
        }

        _trigger.StepGap = TimeSpan.FromMilliseconds((double)_stepGapBox.Value);
        _trigger.FinalHoldEnabled = _finalHoldBox.Checked;
        _trigger.FinalHoldDuration = TimeSpan.FromMilliseconds((double)_finalHoldDurationBox.Value);
        _finalHoldDurationBox.Enabled = _finalHoldBox.Checked;
    }

    private void Record()
    {
        if (_trigger is null)
        {
            return;
        }

        using var dialog = new RecordTriggerDialog(_controllerSource);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK && dialog.RecordedSteps.Count > 0)
        {
            _trigger.Steps = dialog.RecordedSteps;
            ShowSteps(selectedIndex: 0);
        }
    }

    private void AddStep()
    {
        if (_trigger is null)
        {
            return;
        }

        // The new Step starts empty; the user ticks its buttons (an empty Chord does not pass validation).
        _trigger.Steps.Add(default);
        ShowSteps(selectedIndex: _trigger.Steps.Count - 1);
    }

    private void RemoveStep()
    {
        var index = _stepsList.SelectedIndex;
        if (_trigger is null || index < 0)
        {
            return;
        }

        _trigger.Steps.RemoveAt(index);
        ShowSteps(selectedIndex: index);
    }

    private void ClearSteps()
    {
        _trigger?.Steps.Clear();
        ShowSteps(selectedIndex: 0);
    }
}
