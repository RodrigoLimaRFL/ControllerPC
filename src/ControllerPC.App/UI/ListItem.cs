namespace ControllerPC.App.UI;

/// <summary>A value shown in a ComboBox or ListBox under its display text.</summary>
internal sealed record ListItem<T>(T Value, string Text)
{
    public override string ToString() => Text;
}
