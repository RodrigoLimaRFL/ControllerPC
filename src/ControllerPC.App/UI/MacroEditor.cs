using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>
/// Edits a Macro: its Key Combinations in order (Add / Remove / Move Up / Move Down) and its Macro Delay.
/// </summary>
internal sealed class MacroEditor : UserControl
{
    public const int EditorWidth = 310;
    private const int ButtonGap = 6;
    private const int ListWidth = EditorWidth - ClassicControls.ButtonWidth - ButtonGap;
    private const int ListHeight = (3 * ClassicControls.RowHeight) + ClassicControls.ButtonHeight;
    private const int ButtonX = ListWidth + ButtonGap;
    private const int DelayFieldX = 100;
    private const int DelayFieldWidth = 70;
    private const decimal DelayIncrementMilliseconds = 10;

    private readonly List<KeyCombination> _keyCombinations = [];
    private readonly ListBox _list = new()
    {
        Location = Point.Empty,
        Size = new Size(ListWidth, ListHeight),
        IntegralHeight = false,
        HorizontalScrollbar = true,
    };

    private readonly Button _removeButton;
    private readonly Button _upButton;
    private readonly Button _downButton;
    private readonly NumericUpDown _delayBox;
    private bool _loading;

    public MacroEditor()
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        var addButton = ClassicControls.Button("Add...", ButtonX, 0, (_, _) => AddKeyCombination());
        _removeButton = ClassicControls.Button("Remove", ButtonX, ClassicControls.RowHeight, (_, _) => RemoveSelected());
        _upButton = ClassicControls.Button("Move Up", ButtonX, 2 * ClassicControls.RowHeight, (_, _) => MoveSelected(-1));
        _downButton = ClassicControls.Button("Move Down", ButtonX, 3 * ClassicControls.RowHeight, (_, _) => MoveSelected(1));

        var delayY = _list.Bottom + ButtonGap;
        _delayBox = ClassicControls.Number(
            DelayFieldX, delayY, DelayFieldWidth, 0, (decimal)MacroAction.MaxDelay.TotalMilliseconds, DelayIncrementMilliseconds);

        Controls.AddRange(
        [
            _list, addButton, _removeButton, _upButton, _downButton,
            ClassicControls.Label("Macro Delay:", 0, delayY), _delayBox,
            ClassicControls.Label("ms", DelayFieldX + DelayFieldWidth + ButtonGap, delayY),
        ]);
        Size = new Size(EditorWidth, _delayBox.Bottom);

        _list.SelectedIndexChanged += (_, _) => UpdateButtons();
        _delayBox.ValueChanged += (_, _) =>
        {
            if (!_loading)
            {
                MacroChanged?.Invoke(this, EventArgs.Empty);
            }
        };
        Display(null);
    }

    /// <summary>Raised when the user changes the Key Combinations or the Macro Delay.</summary>
    public event EventHandler? MacroChanged;

    /// <summary>The Macro as shown.</summary>
    public MacroAction Macro => new(_keyCombinations, TimeSpan.FromMilliseconds((double)_delayBox.Value));

    /// <summary>Shows <paramref name="macro"/>, or an empty Macro with the default Macro Delay, without raising events.</summary>
    public void Display(MacroAction? macro)
    {
        _loading = true;
        _keyCombinations.Clear();
        _keyCombinations.AddRange(macro?.KeyCombinations ?? []);
        ClassicControls.SetClamped(_delayBox, (decimal)(macro?.Delay ?? MacroAction.DefaultDelay).TotalMilliseconds);
        _loading = false;
        ShowKeyCombinations(selectedIndex: 0);
    }

    private void ShowKeyCombinations(int selectedIndex)
    {
        _list.Items.Clear();
        for (var i = 0; i < _keyCombinations.Count; i++)
        {
            _list.Items.Add(DisplayNames.ForMacroKeyCombination(i, _keyCombinations[i]));
        }

        _list.SelectedIndex = _keyCombinations.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, _keyCombinations.Count - 1);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var index = _list.SelectedIndex;
        _removeButton.Enabled = index >= 0;
        _upButton.Enabled = index > 0;
        _downButton.Enabled = index >= 0 && index < _keyCombinations.Count - 1;
    }

    private void AddKeyCombination()
    {
        using var dialog = new KeyCombinationDialog("Add Key Combination");
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        // Inserted after the selected one, so a Macro can be built in any order.
        var index = _list.SelectedIndex < 0 ? _keyCombinations.Count : _list.SelectedIndex + 1;
        _keyCombinations.Insert(index, dialog.Result);
        Changed(index);
    }

    private void RemoveSelected()
    {
        var index = _list.SelectedIndex;
        if (index >= 0)
        {
            _keyCombinations.RemoveAt(index);
            Changed(index);
        }
    }

    private void MoveSelected(int offset)
    {
        var index = _list.SelectedIndex;
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _keyCombinations.Count)
        {
            return;
        }

        (_keyCombinations[index], _keyCombinations[target]) = (_keyCombinations[target], _keyCombinations[index]);
        Changed(target);
    }

    private void Changed(int selectedIndex)
    {
        ShowKeyCombinations(selectedIndex);
        MacroChanged?.Invoke(this, EventArgs.Empty);
    }
}
