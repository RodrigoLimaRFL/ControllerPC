using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorChordBindingTests
{
    private const ushort Ctrl = 0x11;
    private const ushort Shift = 0x10;
    private const int NudgeStep = 3;

    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton B = ControllerButton.B;
    private const ControllerButton X = ControllerButton.X;
    private const ControllerButton Y = ControllerButton.Y;
    private const ControllerButton Right = ControllerButton.DPadRight;

    private static readonly TimeSpan ChordWait = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);
    private static readonly TimeSpan LongerThanChordWait = TimeSpan.FromSeconds(1);

    private static readonly KeyCombinationAction HoldCtrl = new(new KeyCombination([Ctrl]));
    private static readonly KeyCombinationAction HoldShift = new(new KeyCombination([Shift]));

    private readonly TranslatorHarness _harness = new();

    public DesktopInputTranslatorChordBindingTests()
    {
        _harness.Profile.ChordWait = ChordWait;
        _harness.Profile.Tuning.NudgeStep = NudgeStep;
        _harness
            .Bind(A, new MouseButtonAction(MouseButton.Left))
            .Bind(B, new MouseButtonAction(MouseButton.Right))
            .Bind(X, new MouseButtonAction(MouseButton.Middle))
            .BindChord(A | B, HoldCtrl);
    }

    // Buttons outside every Chord Binding

    [Fact]
    public void Update_ButtonInNoChordBinding_PerformsItsBindingAtOnce()
    {
        _harness.Begin();

        _harness.Hold(X, TimeSpan.Zero);

        Assert.Equal([MouseEvent("down", MouseButton.Middle)], _harness.Sink.ButtonEvents);
    }

    // Chord Wait

    [Fact]
    public void Update_ChordButtonPressed_WaitsBeforePerformingItsBinding()
    {
        _harness.Begin();

        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(A, ChordWait - OneTick);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordButtonHeldForChordWait_PerformsItsBinding()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(A, ChordWait - OneTick);

        _harness.Hold(A, OneTick);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordButtonHeldPastChordWait_KeepsItsBindingHeldUntilReleased()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(A, LongerThanChordWait);
        Assert.Equal([MouseButton.Left], _harness.Sink.MouseButtonsDown);

        _harness.Hold(None);

        Assert.Equal(
            [MouseEvent("down", MouseButton.Left), MouseEvent("up", MouseButton.Left)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordButtonReleasedWithinChordWait_TapsItsBinding()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(A, ChordWait - OneTick);

        _harness.Hold(None, OneTick);

        Assert.Equal(
            [MouseEvent("down", MouseButton.Left), MouseEvent("up", MouseButton.Left)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordButtonWithNudgeTapped_NudgesOneStep()
    {
        _harness.Bind(Right, new NudgeAction(NudgeDirection.Right)).BindChord(Right | X, HoldShift).Begin();
        _harness.Hold(Right, TimeSpan.Zero);

        _harness.Hold(None, TimeSpan.Zero);
        _harness.HoldFor(None, LongerThanChordWait);

        Assert.Equal([(NudgeStep, 0)], _harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_ChordButtonWithProfileSwitchTapped_ReturnsItOnRelease()
    {
        _harness.Bind(Y, new NextProfileAction()).BindChord(Y | X, HoldShift).Begin();

        Assert.Null(_harness.Hold(Y, TimeSpan.Zero));
        Assert.IsType<NextProfileAction>(_harness.Hold(None, TimeSpan.Zero));
    }

    [Fact]
    public void Update_ChordButtonWithProfileSwitchHeldPastChordWait_ReturnsItOnce()
    {
        _harness.Bind(Y, new NextProfileAction()).BindChord(Y | X, HoldShift).Begin();
        _harness.Hold(Y, TimeSpan.Zero);

        var whenWaitRunsOut = _harness.Hold(Y, ChordWait);
        var later = _harness.Hold(Y, ChordWait);
        var onRelease = _harness.Hold(None, TimeSpan.Zero);

        Assert.IsType<NextProfileAction>(whenWaitRunsOut);
        Assert.Null(later);
        Assert.Null(onRelease);
    }

    [Fact]
    public void Update_ChordWaitZero_ChordButtonPerformsItsBindingAtOnce()
    {
        _harness.Profile.ChordWait = TimeSpan.Zero;
        _harness.Begin();

        _harness.Hold(A, TimeSpan.Zero);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordWaitZero_ChordStillActivatesAndReleasesTheSingleBinding()
    {
        _harness.Profile.ChordWait = TimeSpan.Zero;
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal(
            [MouseEvent("down", MouseButton.Left), MouseEvent("up", MouseButton.Left), KeyEvent("down", Ctrl)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_HugeElapsedWhileChordButtonWaits_CountsOnlyMaxUpdateElapsed()
    {
        _harness.Profile.ChordWait = TimeSpan.FromMilliseconds(150);
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A, TimeSpan.FromHours(1));

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    // Chord Bindings

    [Fact]
    public void Update_ChordCompleted_PerformsTheChordAction()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal([PressCall(Ctrl)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_ChordCompleted_CancelsTheWaitingSingleBindings()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(A | B, TimeSpan.Zero);

        _harness.HoldFor(A | B, LongerThanChordWait);

        Assert.Equal([KeyEvent("down", Ctrl)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordButtonsPressedTogether_PerformOnlyTheChordAction()
    {
        _harness.Begin();

        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.HoldFor(A | B, LongerThanChordWait);

        Assert.Equal([KeyEvent("down", Ctrl)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordCompletedAfterSingleBindingWasPerformed_ReleasesIt()
    {
        // E.g. a left click held for a drag, then the Chord is completed.
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(A, LongerThanChordWait);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal([MouseEvent("up", MouseButton.Left), KeyEvent("down", Ctrl)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordHeld_KeepsTheChordActionHeld()
    {
        _harness.Begin();
        _harness.Hold(A | B, TimeSpan.Zero);

        _harness.HoldFor(A | B, LongerThanChordWait);

        Assert.Equal([Ctrl], _harness.Sink.KeysDown);
    }

    [Fact]
    public void Update_OneChordButtonReleased_ReleasesTheChordAction()
    {
        _harness.Begin();
        _harness.Hold(A | B, TimeSpan.Zero);

        _harness.Hold(A, TimeSpan.Zero);

        Assert.Empty(_harness.Sink.KeysDown);
    }

    [Fact]
    public void Update_ButtonStillHeldAfterItsChordReleased_IsConsumed()
    {
        _harness.Begin();
        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Sink.ButtonEvents.Clear();

        _harness.HoldFor(A, LongerThanChordWait);
        _harness.Hold(None, TimeSpan.Zero);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ConsumedButtonPressedAgain_PerformsItsBindingAgain()
    {
        _harness.Begin();
        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.Hold(None, TimeSpan.Zero);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(A, LongerThanChordWait);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordReleasedAndCompletedAgain_ActivatesAgain()
    {
        _harness.Begin();
        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal([Ctrl], _harness.Sink.KeysDown);
    }

    [Fact]
    public void Update_BiggerChordCompleted_ReleasesTheActiveSmallerChord()
    {
        _harness.BindChord(A | B | X, HoldShift).Begin();
        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(A | B | X, TimeSpan.Zero);

        Assert.Equal([KeyEvent("up", Ctrl), KeyEvent("down", Shift)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ChordsOfDifferentSizesCompletedTogether_OnlyTheBiggestActivates()
    {
        _harness.BindChord(A | B | X, HoldShift).Begin();

        _harness.Hold(A | B | X, TimeSpan.Zero);

        Assert.Equal([KeyEvent("down", Shift)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_SmallerChordCompletedWhileBiggerActive_DoesNotActivate()
    {
        _harness.BindChord(A | B | X, HoldShift).BindChord(A | X, new MouseButtonAction(MouseButton.Back)).Begin();
        _harness.Hold(A | B | X, TimeSpan.Zero);
        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(A | B | X, TimeSpan.Zero);

        // A+B+X is completed again and wins; neither A+X nor A+B activates beside it.
        Assert.Equal([KeyEvent("down", Shift)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_BiggerChordReleased_SmallerChordStillHeldDoesNotActivate()
    {
        _harness.BindChord(A | B | X, HoldShift).Begin();
        _harness.Hold(A | B | X, TimeSpan.Zero);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal([KeyEvent("up", Shift)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_OverlappingChordsOfEqualSize_CanBothBeActive()
    {
        _harness.BindChord(B | X, HoldShift).Begin();
        _harness.Hold(A | B, TimeSpan.Zero);

        _harness.Hold(A | B | X, TimeSpan.Zero);

        Assert.Equal([KeyEvent("down", Ctrl), KeyEvent("down", Shift)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_OverlappingChordsOfEqualSize_ReleasingAnUnsharedButtonReleasesOnlyItsChord()
    {
        _harness.BindChord(B | X, HoldShift).Begin();
        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.Hold(A | B | X, TimeSpan.Zero);

        _harness.Hold(B | X, TimeSpan.Zero);

        Assert.Equal([Shift], _harness.Sink.KeysDown);
    }

    [Fact]
    public void Update_ChordWithProfileSwitch_ReturnsItOncePerCompletion()
    {
        _harness.BindChord(B | X, new NextProfileAction()).Begin();

        var onCompletion = _harness.Hold(B | X, TimeSpan.Zero);
        var whileHeld = _harness.Hold(B | X, LongerThanChordWait);

        Assert.IsType<NextProfileAction>(onCompletion);
        Assert.Null(whileHeld);
    }

    [Fact]
    public void Update_ChordWithNudge_NudgesAndRepeatsWhileHeld()
    {
        _harness.BindChord(B | X, new NudgeAction(NudgeDirection.Down)).Begin();

        _harness.Hold(B | X, TimeSpan.Zero);
        _harness.HoldFor(B | X, LongerThanChordWait);

        Assert.True(_harness.Sink.TotalDy > NudgeStep);
        Assert.Equal(0, _harness.Sink.TotalDx);
    }

    [Fact]
    public void Update_ChordButtonHeldAtBegin_CannotCompleteAChord()
    {
        _harness.Begin(heldAtStart: A);

        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.HoldFor(A | B, LongerThanChordWait);

        Assert.Equal([MouseEvent("down", MouseButton.Right)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void ReleaseAll_ReleasesTheActiveChordAction()
    {
        _harness.Begin();
        _harness.Hold(A | B, TimeSpan.Zero);

        _harness.Translator.ReleaseAll();

        Assert.Empty(_harness.Sink.KeysDown);
    }

    [Fact]
    public void Begin_ChordBindingsEditedAfterBegin_AreNotUsedUntilTheNextBegin()
    {
        _harness.Begin();
        _harness.Profile.ChordBindings.Clear();

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal([Ctrl], _harness.Sink.KeysDown);
    }
}
