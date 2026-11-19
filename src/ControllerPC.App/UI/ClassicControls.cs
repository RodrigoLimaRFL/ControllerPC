namespace ControllerPC.App.UI;

/// <summary>
/// Creates plain system controls at fixed positions, in logical pixels at 96 DPI; the owning form scales them.
/// </summary>
internal static class ClassicControls
{
    public const int ButtonWidth = 90;
    public const int ButtonHeight = 25;
    public const int RowHeight = 29;

    // Labels sit lower than the ComboBox or NumericUpDown they caption so the text baselines line up.
    private const int LabelOffset = 3;

    public static Label Label(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y + LabelOffset), AutoSize = true };

    public static Button Button(string text, int x, int y, EventHandler? onClick = null)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(ButtonWidth, ButtonHeight),
            UseVisualStyleBackColor = true,
        };
        if (onClick is not null)
        {
            button.Click += onClick;
        }

        return button;
    }

    public static CheckBox CheckBox(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true, UseVisualStyleBackColor = true };

    public static ComboBox DropDown(int x, int y, int width) =>
        new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, y), Width = width };

    public static NumericUpDown Number(int x, int y, int width, decimal minimum, decimal maximum, decimal increment) =>
        new()
        {
            Location = new Point(x, y),
            Width = width,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            TextAlign = HorizontalAlignment.Right,
        };

    public static GroupBox Group(string text, int x, int y, int width, int height) =>
        new() { Text = text, Location = new Point(x, y), Size = new Size(width, height) };

    /// <summary>Sets <paramref name="box"/> to <paramref name="value"/>, clamped to its range.</summary>
    public static void SetClamped(NumericUpDown box, decimal value) =>
        box.Value = Math.Clamp(value, box.Minimum, box.Maximum);

    /// <summary>Selects the item of <paramref name="box"/> whose value equals <paramref name="value"/>.</summary>
    public static void Select<T>(ComboBox box, T value) =>
        box.SelectedItem = box.Items.OfType<ListItem<T>>().FirstOrDefault(item => Equals(item.Value, value));

    /// <summary>The value of the selected item of <paramref name="box"/>, or <paramref name="fallback"/>.</summary>
    public static T SelectedValue<T>(ComboBox box, T fallback) =>
        box.SelectedItem is ListItem<T> item ? item.Value : fallback;
}
