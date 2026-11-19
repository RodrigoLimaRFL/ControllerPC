using ControllerPC.Core.Output;

namespace ControllerPC.App.UI;

/// <summary>
/// A text box that captures a Key Combination from the physical keyboard instead of accepting text.
/// Pressing a key captures it together with the modifiers held; pressing and releasing modifiers alone captures
/// just those modifiers. Keys are taken in <see cref="ProcessCmdKey"/>, before the form or a tab control can use
/// Enter, Esc or Ctrl+Tab.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Plain Tab (no modifier held) is not captured: it moves the focus as usual, so the box can be left with
/// the keyboard. Tab with a modifier (e.g. Alt+Tab) is captured.</item>
/// <item>Windows keeps many Win+key combinations for itself, so the Win key is often missing from what reaches the
/// box; <see cref="SetWin"/> lets an accompanying "Win" check box add it.</item>
/// <item>Print Screen cannot be captured: Windows sends no key-down message for it to applications.</item>
/// </list>
/// </remarks>
internal sealed class KeyCombinationCaptureBox : TextBox
{
    private const string EmptyPrompt = "(click here and press keys)";
    private const string PendingSuffix = "+...";

    private readonly HashSet<Keys> _heldModifiers = [];
    private bool _onlyModifiersPressed;

    public KeyCombinationCaptureBox()
    {
        ReadOnly = true;
        ShortcutsEnabled = false;

        // Read-only boxes default to the dialog color; this one is meant to be typed into.
        BackColor = SystemColors.Window;
        ShowValue();
    }

    /// <summary>Raised when the user captures a new Key Combination or toggles Win.</summary>
    public event EventHandler? ValueChanged;

    public KeyCombination Value { get; private set; } = new KeyCombination([]);

    /// <summary>Shows <paramref name="keys"/> without raising <see cref="ValueChanged"/>.</summary>
    public void SetValue(KeyCombination keys)
    {
        Value = keys;
        ShowValue();
    }

    /// <summary>Adds or removes the Win key from the current value.</summary>
    public void SetWin(bool includeWin)
    {
        if (KeyCombinationComposer.HasWin(Value) != includeWin)
        {
            Commit(KeyCombinationComposer.WithWin(Value, includeWin));
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (keyData == Keys.Tab && _heldModifiers.Count == 0)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        if (KeyCombinationComposer.ToModifier(key) is { } modifier)
        {
            if (_heldModifiers.Count == 0)
            {
                _onlyModifiersPressed = true;
            }

            _heldModifiers.Add(modifier);
            Text = DisplayNames.ForKeyCombination(KeyCombinationComposer.Compose(_heldModifiers, null)) + PendingSuffix;
            return true;
        }

        var modifiers = KeyCombinationComposer.ModifiersOf(keyData).Concat(_heldModifiers);
        _onlyModifiersPressed = false;
        Commit(KeyCombinationComposer.Compose(modifiers, key));
        return true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (KeyCombinationComposer.ToModifier(e.KeyCode) is { } modifier)
        {
            if (_onlyModifiersPressed)
            {
                _onlyModifiersPressed = false;
                Commit(KeyCombinationComposer.Compose(_heldModifiers, null));
            }

            _heldModifiers.Remove(modifier);
            if (_heldModifiers.Count == 0)
            {
                ShowValue();
            }
        }

        // Swallow every release, so a lone Alt does not activate a menu.
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    protected override void OnLeave(EventArgs e)
    {
        _heldModifiers.Clear();
        _onlyModifiersPressed = false;
        ShowValue();
        base.OnLeave(e);
    }

    private void Commit(KeyCombination keys)
    {
        SetValue(keys);
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowValue() =>
        Text = Value.IsEmpty ? EmptyPrompt : DisplayNames.ForKeyCombination(Value);
}
