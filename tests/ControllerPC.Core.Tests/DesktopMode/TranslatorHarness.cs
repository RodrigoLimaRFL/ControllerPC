using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Tests.Fakes;

namespace ControllerPC.Core.Tests.DesktopMode;

/// <summary>
/// A <see cref="DesktopInputTranslator"/> wired to a <see cref="RecordingInputSink"/>, with a Profile whose tuning
/// gives round numbers: full deflection for one second moves 1000 pixels or scrolls 10 notches.
/// </summary>
internal sealed class TranslatorHarness
{
    /// <summary>A typical time between polls.</summary>
    public static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(10);

    public const double Deadzone = 0.2;
    public const double CursorSpeed = 1000;
    public const double ScrollSpeed = 10;

    public TranslatorHarness()
    {
        Translator = new DesktopInputTranslator(Sink);
    }

    public RecordingInputSink Sink { get; } = new();

    public DesktopInputTranslator Translator { get; }

    public Profile Profile { get; } = new()
    {
        RightStickRole = RightStickRole.Scroll,
        Tuning = new MovementTuning
        {
            Deadzone = Deadzone,
            CursorSpeed = CursorSpeed,
            AccelerationEnabled = false,
            NudgeStep = 1,
            ScrollSpeed = ScrollSpeed,
        },
    };

    public TranslatorHarness Bind(ControllerButton button, ButtonAction action)
    {
        Profile.Bindings[button] = action;
        return this;
    }

    public void Begin(ControllerButton heldAtStart = ControllerButton.None) => Translator.Begin(Profile, heldAtStart);

    /// <summary>Updates with exactly <paramref name="buttons"/> held and both sticks centered.</summary>
    public ProfileSwitchAction? Hold(ControllerButton buttons, TimeSpan elapsed) =>
        Translator.Update(new ControllerState(buttons, StickPosition.Center, StickPosition.Center), elapsed);

    /// <inheritdoc cref="Hold(ControllerButton, TimeSpan)"/>
    public ProfileSwitchAction? Hold(ControllerButton buttons) => Hold(buttons, Poll);

    /// <summary>
    /// Holds exactly <paramref name="buttons"/> for <paramref name="duration"/>, split into updates no longer than
    /// <see cref="DesktopInputTranslator.MaxUpdateElapsed"/> so none of it is clamped away.
    /// </summary>
    public void HoldFor(ControllerButton buttons, TimeSpan duration)
    {
        var remaining = duration;
        while (remaining > TimeSpan.Zero)
        {
            var step = remaining < DesktopInputTranslator.MaxUpdateElapsed ? remaining : DesktopInputTranslator.MaxUpdateElapsed;
            Hold(buttons, step);
            remaining -= step;
        }
    }

    public void Sticks(StickPosition left, StickPosition right, TimeSpan elapsed) =>
        Translator.Update(new ControllerState(ControllerButton.None, left, right), elapsed);
}
