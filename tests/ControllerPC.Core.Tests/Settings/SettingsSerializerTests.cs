using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;
using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Tests.Settings;

public class SettingsSerializerTests
{
    public static TheoryData<ButtonAction> EveryActionType => new()
    {
        NoAction.Instance,
        new MouseButtonAction(MouseButton.Forward),
        new KeyCombinationAction(new KeyCombination([0x11, 0x12, 0x09])),
        new NudgeAction(NudgeDirection.Left),
        new NextProfileAction(),
        new SwitchToProfileAction(Guid.Parse("6f1c2d3e-4a5b-4c6d-8e7f-901a2b3c4d5e")),
    };

    [Fact]
    public void Serialize_DefaultSettings_RoundTripsToIdenticalJson()
    {
        var json = SettingsSerializer.Serialize(DefaultSettings.Create());

        Assert.Equal(json, SettingsSerializer.Serialize(SettingsSerializer.Deserialize(json)));
    }

    [Fact]
    public void Serialize_WritesIndentedJson()
    {
        var json = SettingsSerializer.Serialize(DefaultSettings.Create());

        Assert.Contains("\n  \"", json);
    }

    [Fact]
    public void Serialize_WritesFlagsChordAsButtonNames()
    {
        var json = SettingsSerializer.Serialize(DefaultSettings.Create());

        Assert.Contains("\"buttons\": \"Start, LeftShoulder, RightShoulder\"", json);
    }

    [Fact]
    public void Serialize_WritesBindingKeysAsButtonNames()
    {
        var json = SettingsSerializer.Serialize(DefaultSettings.Create());

        Assert.Contains("\"DPadUp\": {", json);
        Assert.Contains("\"LeftShoulder\": {", json);
    }

    [Fact]
    public void Serialize_WritesActionTypeDiscriminator()
    {
        var json = SettingsSerializer.Serialize(DefaultSettings.Create());

        Assert.Contains("\"type\": \"mouse\"", json);
        Assert.Contains("\"button\": \"Left\"", json);
    }

    [Fact]
    public void Serialize_DoesNotWriteDerivedActiveProfile()
    {
        var json = SettingsSerializer.Serialize(DefaultSettings.Create());

        Assert.DoesNotContain("\"activeProfile\"", json);
    }

    [Fact]
    public void Clone_SettingsWithNoProfiles_Succeeds()
    {
        var settings = DefaultSettings.Create();
        settings.Profiles.Clear();

        Assert.Empty(SettingsSerializer.Clone(settings).Profiles);
    }

    [Fact]
    public void Deserialize_ReadsFlagsChord()
    {
        var settings = RoundTrip(DefaultSettings.Create());

        Assert.Equal(new Chord(TestSettings.DefaultTriggerChord), Assert.Single(settings.BaseEntryTrigger.Steps));
    }

    [Fact]
    public void Deserialize_ReadsMultiButtonBindingKey()
    {
        // Invalid as a Binding, but the serializer must not lose it so the validator can report it.
        const ControllerButton chord = ControllerButton.A | ControllerButton.X;
        var settings = DefaultSettings.Create();
        settings.ActiveProfile.Bindings[chord] = new NudgeAction(NudgeDirection.Up);

        var roundTripped = RoundTrip(settings);

        Assert.Equal(new NudgeAction(NudgeDirection.Up), roundTripped.ActiveProfile.Bindings[chord]);
    }

    [Theory]
    [MemberData(nameof(EveryActionType))]
    public void RoundTrip_PreservesAction(ButtonAction action)
    {
        var settings = DefaultSettings.Create();
        settings.ActiveProfile.Bindings[ControllerButton.X] = action;

        var roundTripped = RoundTrip(settings);

        Assert.Equal(action, roundTripped.ActiveProfile.Bindings[ControllerButton.X]);
    }

    [Fact]
    public void RoundTrip_PreservesAllBindings()
    {
        var settings = DefaultSettings.Create();

        Assert.Equal(settings.ActiveProfile.Bindings, RoundTrip(settings).ActiveProfile.Bindings);
    }

    [Fact]
    public void RoundTrip_PreservesTriggerOverridesAndTiming()
    {
        var settings = DefaultSettings.Create();
        var profile = TestSettings.AddProfile(settings, "Games");
        profile.EntryTriggerOverride = TestSettings.Trigger(ControllerButton.Back, ControllerButton.A | ControllerButton.B);
        profile.EntryTriggerOverride.StepGap = TimeSpan.FromMilliseconds(750);
        profile.EntryTriggerOverride.FinalHoldEnabled = true;
        profile.EntryTriggerOverride.FinalHoldDuration = TimeSpan.FromSeconds(2);

        var roundTripped = RoundTrip(settings).Profiles[1];

        Assert.Null(roundTripped.ExitTriggerOverride);
        var entryOverride = Assert.IsType<ModeTrigger>(roundTripped.EntryTriggerOverride);
        Assert.Equal(profile.EntryTriggerOverride.Steps, entryOverride.Steps);
        Assert.Equal(TimeSpan.FromMilliseconds(750), entryOverride.StepGap);
        Assert.True(entryOverride.FinalHoldEnabled);
        Assert.Equal(TimeSpan.FromSeconds(2), entryOverride.FinalHoldDuration);
    }

    [Fact]
    public void RoundTrip_PreservesProfileIdentityAndActiveProfile()
    {
        var settings = DefaultSettings.Create();
        var profile = TestSettings.AddProfile(settings, "Work");
        settings.ActiveProfileId = profile.Id;

        var roundTripped = RoundTrip(settings);

        Assert.Equal(settings.Profiles.Select(p => (p.Id, p.Name)), roundTripped.Profiles.Select(p => (p.Id, p.Name)));
        Assert.Equal(profile.Id, roundTripped.ActiveProfileId);
    }

    [Fact]
    public void RoundTrip_PreservesTuningAndRightStickRole()
    {
        var settings = DefaultSettings.Create();
        var profile = settings.ActiveProfile;
        profile.RightStickRole = RightStickRole.Cursor;
        profile.Tuning = new MovementTuning
        {
            Deadzone = 0.15,
            CursorSpeed = 800,
            AccelerationEnabled = false,
            NudgeStep = 3,
            ScrollSpeed = 4.5,
        };

        var roundTripped = RoundTrip(settings).ActiveProfile;

        Assert.Equal(RightStickRole.Cursor, roundTripped.RightStickRole);
        Assert.Equal(0.15, roundTripped.Tuning.Deadzone);
        Assert.Equal(800, roundTripped.Tuning.CursorSpeed);
        Assert.False(roundTripped.Tuning.AccelerationEnabled);
        Assert.Equal(3, roundTripped.Tuning.NudgeStep);
        Assert.Equal(4.5, roundTripped.Tuning.ScrollSpeed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{\"version\": \"one\"}")]
    public void Deserialize_Garbage_ThrowsFormatException(string json)
    {
        Assert.Throws<FormatException>(() => SettingsSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_NullLiteral_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => SettingsSerializer.Deserialize("null"));
    }

    [Fact]
    public void Deserialize_UnknownActionType_ThrowsFormatException()
    {
        var json = DefaultJson().Replace("\"type\": \"mouse\"", "\"type\": \"teleport\"");

        Assert.Throws<FormatException>(() => SettingsSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_UnknownButtonName_ThrowsFormatException()
    {
        var json = DefaultJson().Replace("\"DPadUp\"", "\"Turbo\"");

        Assert.Throws<FormatException>(() => SettingsSerializer.Deserialize(json));
    }

    [Theory]
    [InlineData("{\"profiles\": null}")]
    [InlineData("{\"profiles\": [null]}")]
    [InlineData("{\"baseEntryTrigger\": null}")]
    [InlineData("{\"baseExitTrigger\": {\"steps\": null}}")]
    [InlineData("{\"profiles\": [{\"name\": null}]}")]
    [InlineData("{\"profiles\": [{\"tuning\": null}]}")]
    [InlineData("{\"profiles\": [{\"bindings\": null}]}")]
    [InlineData("{\"profiles\": [{\"bindings\": {\"A\": null}}]}")]
    [InlineData("{\"profiles\": [{\"bindings\": {\"A\": {\"type\": \"keys\"}}}]}")]
    [InlineData("{\"profiles\": [{\"entryTriggerOverride\": {\"steps\": null}}]}")]
    [InlineData("{\"profiles\": [{\"exitTriggerOverride\": {\"steps\": null}}]}")]
    public void Deserialize_MissingRequiredValue_ThrowsFormatException(string json)
    {
        Assert.Throws<FormatException>(() => SettingsSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_MissingOptionalValues_UsesModelDefaults()
    {
        var settings = SettingsSerializer.Deserialize("{\"profiles\": [{\"name\": \"Solo\"}]}");

        Assert.Equal(AppSettings.CurrentVersion, settings.Version);
        Assert.Equal("Solo", Assert.Single(settings.Profiles).Name);
        Assert.Equal(ModeTrigger.DefaultStepGap, settings.BaseEntryTrigger.StepGap);
    }

    [Fact]
    public void Clone_ReturnsIndependentCopy()
    {
        var original = DefaultSettings.Create();

        var clone = SettingsSerializer.Clone(original);
        clone.ActiveProfile.Name = "Edited";
        clone.BaseEntryTrigger.Steps.Clear();

        Assert.Equal("Default", original.ActiveProfile.Name);
        Assert.NotEmpty(original.BaseEntryTrigger.Steps);
    }

    private static AppSettings RoundTrip(AppSettings settings) =>
        SettingsSerializer.Deserialize(SettingsSerializer.Serialize(settings));

    private static string DefaultJson() => SettingsSerializer.Serialize(DefaultSettings.Create());
}
