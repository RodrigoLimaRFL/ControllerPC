using ControllerPC.Core.Controller;
using ControllerPC.Core.Triggers;

namespace ControllerPC.App.UI;

/// <summary>Picks the buttons of a Chord with one check box per controller button, in a grid.</summary>
internal sealed class ChordPicker : UserControl
{
    private const int Columns = 4;
    private const int ColumnWidth = 102;
    private const int RowHeight = 23;
    private const int CellGap = 2;

    /// <summary>The width of the grid, in logical pixels, for laying out controls around it.</summary>
    public const int GridWidth = Columns * ColumnWidth;

    private readonly Dictionary<ControllerButton, CheckBox> _boxes = [];
    private bool _loading;

    public ChordPicker()
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        var buttons = ControllerButtons.All;
        for (var i = 0; i < buttons.Count; i++)
        {
            var box = ClassicControls.CheckBox(
                DisplayNames.ForButton(buttons[i]), (i % Columns) * ColumnWidth, (i / Columns) * RowHeight);

            // A fixed size keeps the grid regular when it is scaled; auto-sized boxes overlapped their neighbors.
            box.AutoSize = false;
            box.Size = new Size(ColumnWidth - CellGap, RowHeight - CellGap);
            box.CheckedChanged += (_, _) => OnBoxChanged();
            _boxes[buttons[i]] = box;
            Controls.Add(box);
        }

        var rows = (buttons.Count + Columns - 1) / Columns;
        Size = new Size(GridWidth, rows * RowHeight);
    }

    /// <summary>Raised when the user ticks or unticks a button.</summary>
    public event EventHandler? ChordChanged;

    /// <summary>The ticked buttons as a Chord (empty when none is ticked).</summary>
    public Chord Chord => new(_boxes.Where(pair => pair.Value.Checked).Aggregate(ControllerButton.None, (all, pair) => all | pair.Key));

    /// <summary>Ticks exactly the buttons of <paramref name="chord"/>, without raising <see cref="ChordChanged"/>.</summary>
    public void SetChord(Chord chord)
    {
        _loading = true;
        foreach (var (button, box) in _boxes)
        {
            box.Checked = chord.Buttons.HasFlag(button);
        }

        _loading = false;
    }

    private void OnBoxChanged()
    {
        if (!_loading)
        {
            ChordChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
