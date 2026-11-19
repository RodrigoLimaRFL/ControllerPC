using ControllerPC.Core.Profiles;

namespace ControllerPC.App.UI;

/// <summary>Edits a Profile's Right Stick Role and movement tuning.</summary>
internal sealed class MovementEditor : UserControl
{
    private const int FieldX = 190;
    private const int FieldWidth = 90;
    private const int UnitX = FieldX + FieldWidth + 6;
    private const decimal PercentPerUnit = 100;

    // Ranges offered in the UI; the domain limits (checked by SettingsValidator) are wider.
    private const decimal MaxDeadzonePercent = 95;
    private const decimal MinCursorSpeed = 50;
    private const decimal MaxCursorSpeed = 10000;
    private const decimal CursorSpeedIncrement = 50;
    private const decimal MaxNudgeStep = 100;
    private const decimal MinScrollSpeed = 0.5m;
    private const decimal MaxScrollSpeed = 100;
    private const decimal ScrollSpeedIncrement = 0.5m;

    private readonly ComboBox _rightStickRoleBox = ClassicControls.DropDown(FieldX, 0, FieldWidth + 60);
    private readonly NumericUpDown _deadzoneBox = ClassicControls.Number(FieldX, Row(1), FieldWidth, 0, MaxDeadzonePercent, 1);
    private readonly NumericUpDown _cursorSpeedBox =
        ClassicControls.Number(FieldX, Row(2), FieldWidth, MinCursorSpeed, MaxCursorSpeed, CursorSpeedIncrement);

    private readonly CheckBox _accelerationBox = ClassicControls.CheckBox("Acceleration (finer control near the center)", 0, Row(3));
    private readonly NumericUpDown _nudgeStepBox = ClassicControls.Number(FieldX, Row(4), FieldWidth, 1, MaxNudgeStep, 1);
    private readonly NumericUpDown _scrollSpeedBox =
        ClassicControls.Number(FieldX, Row(5), FieldWidth, MinScrollSpeed, MaxScrollSpeed, ScrollSpeedIncrement);

    private Profile _profile = new();
    private bool _loading;

    public MovementEditor()
    {
        AutoScaleMode = AutoScaleMode.Inherit;
        Size = new Size(UnitX + 110, Row(6));
        _scrollSpeedBox.DecimalPlaces = 1;
        _rightStickRoleBox.Items.AddRange(
            [.. Enum.GetValues<RightStickRole>().Select(role => new ListItem<RightStickRole>(role, DisplayNames.ForRightStickRole(role)))]);

        Controls.AddRange(
        [
            ClassicControls.Label("Right Stick Role:", 0, 0), _rightStickRoleBox,
            ClassicControls.Label("Deadzone:", 0, Row(1)), _deadzoneBox, ClassicControls.Label("%", UnitX, Row(1)),
            ClassicControls.Label("Cursor speed:", 0, Row(2)), _cursorSpeedBox, ClassicControls.Label("pixels/s", UnitX, Row(2)),
            _accelerationBox,
            ClassicControls.Label("Nudge step:", 0, Row(4)), _nudgeStepBox, ClassicControls.Label("pixels", UnitX, Row(4)),
            ClassicControls.Label("Scroll speed:", 0, Row(5)), _scrollSpeedBox, ClassicControls.Label("notches/s", UnitX, Row(5)),
        ]);

        _rightStickRoleBox.SelectedIndexChanged += (_, _) => Store();
        _deadzoneBox.ValueChanged += (_, _) => Store();
        _cursorSpeedBox.ValueChanged += (_, _) => Store();
        _accelerationBox.CheckedChanged += (_, _) => Store();
        _nudgeStepBox.ValueChanged += (_, _) => Store();
        _scrollSpeedBox.ValueChanged += (_, _) => Store();
    }

    /// <summary>Edits <paramref name="profile"/>'s Right Stick Role and tuning in place.</summary>
    public void Edit(Profile profile)
    {
        _loading = true;
        _profile = profile;
        var tuning = profile.Tuning;
        ClassicControls.Select(_rightStickRoleBox, profile.RightStickRole);
        ClassicControls.SetClamped(_deadzoneBox, (decimal)tuning.Deadzone * PercentPerUnit);
        ClassicControls.SetClamped(_cursorSpeedBox, (decimal)tuning.CursorSpeed);
        _accelerationBox.Checked = tuning.AccelerationEnabled;
        ClassicControls.SetClamped(_nudgeStepBox, tuning.NudgeStep);
        ClassicControls.SetClamped(_scrollSpeedBox, (decimal)tuning.ScrollSpeed);
        _loading = false;
    }

    private static int Row(int index) => index * ClassicControls.RowHeight;

    // Writes every field, not just the one touched: a stored value outside the UI range was clamped on display,
    // and once the user edits anything the Profile should match what they see.
    private void Store()
    {
        if (_loading)
        {
            return;
        }

        _profile.RightStickRole = ClassicControls.SelectedValue(_rightStickRoleBox, _profile.RightStickRole);
        var tuning = _profile.Tuning;
        tuning.Deadzone = (double)(_deadzoneBox.Value / PercentPerUnit);
        tuning.CursorSpeed = (double)_cursorSpeedBox.Value;
        tuning.AccelerationEnabled = _accelerationBox.Checked;
        tuning.NudgeStep = (int)_nudgeStepBox.Value;
        tuning.ScrollSpeed = (double)_scrollSpeedBox.Value;
    }
}
