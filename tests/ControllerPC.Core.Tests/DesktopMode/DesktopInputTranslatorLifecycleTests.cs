using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorLifecycleTests
{
    private const ushort Ctrl = 0x11;
    private const ushort C = 0x43;

    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton B = ControllerButton.B;
    private const ControllerButton Y = ControllerButton.Y;
    private const ControllerButton Right = ControllerButton.DPadRight;

    private static readonly StickPosition FullRight = new(1, 0);

    private readonly TranslatorHarness _harness = new();

    public DesktopInputTranslatorLifecycleTests()
    {
        _harness
            .Bind(A, new MouseButtonAction(MouseButton.Left))
            .Bind(B, new KeyCombinationAction(new KeyCombination([Ctrl, C])))
            .Bind(Y, new NextProfileAction())
            .Bind(Right, new NudgeAction(NudgeDirection.Right));
    }

    [Fact]
    public void Update_BeforeBegin_DoesNothing()
    {
        var profileSwitch = _harness.Translator.Update(new ControllerState(A | B | Y | Right, FullRight, FullRight), TranslatorHarness.Poll);

        Assert.Null(profileSwitch);
        AssertNoInputSent();
    }

    [Fact]
    public void Update_AfterReleaseAll_DoesNothing()
    {
        _harness.Begin();
        _harness.Translator.ReleaseAll();

        var profileSwitch = _harness.Translator.Update(new ControllerState(A | B | Y | Right, FullRight, FullRight), TranslatorHarness.Poll);

        Assert.Null(profileSwitch);
        AssertNoInputSent();
    }

    [Fact]
    public void ReleaseAll_ReleasesEveryHeldKeyAndMouseButton()
    {
        _harness.Begin();
        _harness.Hold(A | B);

        _harness.Translator.ReleaseAll();

        Assert.Empty(_harness.Sink.KeysDown);
        Assert.Empty(_harness.Sink.MouseButtonsDown);
    }

    [Fact]
    public void ReleaseAll_ReleasesKeysInReversePressOrder()
    {
        _harness.Begin();
        _harness.Hold(B);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Translator.ReleaseAll();

        Assert.Equal([KeyEvent("up", C), KeyEvent("up", Ctrl)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void ReleaseAll_SharedKeyHeldByTwoBindings_ReleasesItOnce()
    {
        _harness.Bind(ControllerButton.X, new KeyCombinationAction(new KeyCombination([Ctrl])));
        _harness.Begin();
        _harness.Hold(B | ControllerButton.X);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Translator.ReleaseAll();

        Assert.Equal([KeyEvent("up", C), KeyEvent("up", Ctrl)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void ReleaseAll_ReleasesEveryHeldKeyInOneCall()
    {
        _harness.Bind(ControllerButton.X, new KeyCombinationAction(new KeyCombination([Ctrl])));
        _harness.Begin();
        _harness.Hold(ControllerButton.X);
        _harness.Hold(ControllerButton.X | B);
        _harness.Sink.KeyCalls.Clear();

        _harness.Translator.ReleaseAll();

        Assert.Equal([ReleaseCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void ReleaseAll_ThenButtonsLetGo_SendsNoFurtherReleases()
    {
        _harness.Begin();
        _harness.Hold(A | B);
        _harness.Translator.ReleaseAll();
        _harness.Sink.ButtonEvents.Clear();

        _harness.Begin();
        _harness.Hold(None);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void ReleaseAll_WithNothingHeld_SendsNothing()
    {
        _harness.Translator.ReleaseAll();

        AssertNoInputSent();
    }

    [Fact]
    public void Begin_ReleasesWhatThePreviousProfileHeld()
    {
        _harness.Begin();
        _harness.Hold(A | B);

        _harness.Translator.Begin(new Profile(), A | B);

        Assert.Empty(_harness.Sink.KeysDown);
        Assert.Empty(_harness.Sink.MouseButtonsDown);
    }

    [Fact]
    public void Update_ButtonHeldAtBegin_IsNotPressed()
    {
        _harness.Begin(heldAtStart: A);

        _harness.Hold(A);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ButtonHeldAtBeginReleased_SendsNoRelease()
    {
        _harness.Begin(heldAtStart: A);
        _harness.Hold(A);

        _harness.Hold(None);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ButtonHeldAtBeginReleasedThenPressed_IsPressed()
    {
        _harness.Begin(heldAtStart: A);
        _harness.Hold(None);

        _harness.Hold(A);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_OtherButtonPressedWhileButtonHeldAtBeginStillHeld_IsPressed()
    {
        _harness.Begin(heldAtStart: A);

        _harness.Hold(A | B);

        Assert.Equal([KeyEvent("down", Ctrl), KeyEvent("down", C)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ProfileSwitchHeldAtBegin_IsNotReturned()
    {
        _harness.Begin(heldAtStart: Y);

        Assert.Null(_harness.Hold(Y));
    }

    [Fact]
    public void Update_NudgeHeldAtBegin_DoesNotNudge()
    {
        _harness.Begin(heldAtStart: Right);

        _harness.HoldFor(Right, TimeSpan.FromSeconds(1));

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Begin_AgainWhileNudgeHeld_StopsItRepeating()
    {
        _harness.Begin();
        _harness.Hold(Right, TimeSpan.Zero);
        _harness.Sink.CursorMoves.Clear();

        _harness.Begin(heldAtStart: Right);
        _harness.HoldFor(Right, TimeSpan.FromSeconds(1));

        Assert.Empty(_harness.Sink.CursorMoves);
    }

    private void AssertNoInputSent()
    {
        Assert.Empty(_harness.Sink.CursorMoves);
        Assert.Empty(_harness.Sink.Scrolls);
        Assert.Empty(_harness.Sink.ButtonEvents);
    }
}
