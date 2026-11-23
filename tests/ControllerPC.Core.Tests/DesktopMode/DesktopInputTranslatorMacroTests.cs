using ControllerPC.Core.Controller;
using ControllerPC.Core.DesktopMode;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorMacroTests
{
    private const ushort Ctrl = 0x11;
    private const ushort C = 0x43;
    private const ushort V = 0x56;

    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton B = ControllerButton.B;
    private const ControllerButton X = ControllerButton.X;

    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan Tap = RunningMacro.TapDuration;
    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);
    private static readonly TimeSpan LongerThanTheMacro = TimeSpan.FromSeconds(1);

    private static readonly KeyCombination Copy = new([Ctrl, C]);
    private static readonly KeyCombination Paste = new([Ctrl, V]);
    private static readonly MacroAction CopyThenPaste = new([Copy, Paste], Delay);

    private static readonly string[] WholeRun =
    [
        PressCall(Ctrl, C), ReleaseCall(Ctrl, C), PressCall(Ctrl, V), ReleaseCall(Ctrl, V),
    ];

    private readonly TranslatorHarness _harness = new();

    public DesktopInputTranslatorMacroTests()
    {
        _harness.Bind(A, CopyThenPaste);
    }

    // Timing

    [Fact]
    public void Update_MacroBindingPressed_PressesTheFirstKeyCombinationAtOnce()
    {
        _harness.Begin();

        _harness.Hold(A, TimeSpan.Zero);

        Assert.Equal([PressCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_FirstKeyCombinationHeldShorterThanTheTap_IsNotReleasedYet()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A, Tap - OneTick);

        Assert.Equal([PressCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_FirstKeyCombinationHeldForTheTap_IsReleased()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(A, Tap - OneTick);

        _harness.Hold(A, OneTick);

        Assert.Equal([PressCall(Ctrl, C), ReleaseCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_WithinTheMacroDelay_DoesNotPressTheNextKeyCombination()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(A, Tap);

        _harness.Hold(A, Delay - OneTick);

        Assert.Equal([PressCall(Ctrl, C), ReleaseCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_AfterTheMacroDelay_PressesTheNextKeyCombination()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(A, Tap);
        _harness.Hold(A, Delay - OneTick);

        _harness.Hold(A, OneTick);

        Assert.Equal([PressCall(Ctrl, C), ReleaseCall(Ctrl, C), PressCall(Ctrl, V)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroRunToTheEnd_TapsEveryKeyCombinationInOrder()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.HoldFor(A, LongerThanTheMacro);

        Assert.Equal(WholeRun, _harness.Sink.KeyCalls);
        Assert.Empty(_harness.Sink.KeysDown);
    }

    [Fact]
    public void Update_SeveralChangesFallDueInOneUpdate_PerformsThemAll()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A, Tap + Delay);

        Assert.Equal([PressCall(Ctrl, C), ReleaseCall(Ctrl, C), PressCall(Ctrl, V)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroDelayZero_PressesTheNextKeyCombinationWhenReleasingThePrevious()
    {
        _harness.Bind(A, new MacroAction([Copy, Paste], TimeSpan.Zero)).Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A, Tap);

        Assert.Equal([PressCall(Ctrl, C), ReleaseCall(Ctrl, C), PressCall(Ctrl, V)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_HugeElapsed_RunsOnlyAsFarAsMaxUpdateElapsed()
    {
        // 100 ms covers the first tap (30 ms) and the Macro Delay (50 ms), but not the second tap.
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A, TimeSpan.FromHours(1));

        Assert.Equal([PressCall(Ctrl, C), ReleaseCall(Ctrl, C), PressCall(Ctrl, V)], _harness.Sink.KeyCalls);
    }

    // Presses and releases of the button

    [Fact]
    public void Update_MacroButtonHeld_DoesNotRepeatTheMacro()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.HoldFor(A, LongerThanTheMacro * 3);

        Assert.Equal(WholeRun, _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroButtonReleasedEarly_MacroRunsToTheEnd()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(None, TimeSpan.Zero);

        _harness.HoldFor(None, LongerThanTheMacro);

        Assert.Equal(WholeRun, _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroButtonPressedAgainWhileRunning_IsIgnored()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(None, OneTick);

        _harness.Hold(A, OneTick);
        _harness.HoldFor(None, LongerThanTheMacro);

        Assert.Equal(WholeRun, _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroButtonPressedAgainAfterTheMacroEnded_RunsItAgain()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.HoldFor(None, LongerThanTheMacro);
        _harness.Sink.KeyCalls.Clear();

        _harness.Hold(A, TimeSpan.Zero);

        Assert.Equal([PressCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_TwoMacroButtons_RunIndependently()
    {
        _harness.Bind(B, new MacroAction([new KeyCombination([V])], Delay)).Begin();

        _harness.Hold(A | B, TimeSpan.Zero);

        Assert.Equal([PressCall(Ctrl, C), PressCall(V)], _harness.Sink.KeyCalls);
    }

    // Keys shared with other Bindings

    [Fact]
    public void Update_MacroKeyHeldByAnotherBinding_StaysDownThroughTheMacro()
    {
        _harness.Bind(B, new KeyCombinationAction(new KeyCombination([Ctrl]))).Begin();
        _harness.Hold(B, TimeSpan.Zero);

        _harness.Hold(A | B, TimeSpan.Zero);
        _harness.HoldFor(B, LongerThanTheMacro);

        Assert.Equal(
            [PressCall(Ctrl), PressCall(C), ReleaseCall(C), PressCall(V), ReleaseCall(V)],
            _harness.Sink.KeyCalls);
        Assert.Equal([Ctrl], _harness.Sink.KeysDown);
    }

    // Stopping

    [Fact]
    public void ReleaseAll_WhileMacroHoldsKeys_ReleasesThem()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Translator.ReleaseAll();

        Assert.Empty(_harness.Sink.KeysDown);
    }

    [Fact]
    public void ReleaseAll_WhileMacroRuns_StopsIt()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Translator.ReleaseAll();
        _harness.Sink.KeyCalls.Clear();

        _harness.Begin();
        _harness.HoldFor(None, LongerThanTheMacro);

        Assert.Empty(_harness.Sink.KeyCalls);
    }

    [Fact]
    public void Begin_DuringTheMacroDelay_StopsTheMacro()
    {
        _harness.Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(A, Tap);
        _harness.Sink.KeyCalls.Clear();

        _harness.Begin(heldAtStart: A);
        _harness.HoldFor(A, LongerThanTheMacro);

        Assert.Empty(_harness.Sink.KeyCalls);
    }

    // Chord Bindings

    [Fact]
    public void Update_ChordBindingWithMacro_RunsItWhenTheChordCompletes()
    {
        _harness.BindChord(B | X, CopyThenPaste).Begin();

        _harness.Hold(B | X, TimeSpan.Zero);
        _harness.HoldFor(B | X, LongerThanTheMacro);

        Assert.Equal(WholeRun, _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroOnChordButtonTapped_RunsTheMacro()
    {
        _harness.BindChord(A | X, NoAction.Instance).Begin();
        _harness.Hold(A, TimeSpan.Zero);
        Assert.Empty(_harness.Sink.KeyCalls);

        _harness.Hold(None, OneTick);

        Assert.Equal([PressCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroOnChordButtonTapped_RunsToTheEnd()
    {
        _harness.BindChord(A | X, NoAction.Instance).Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(None, OneTick);

        _harness.HoldFor(None, LongerThanTheMacro);

        Assert.Equal(WholeRun, _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroOnChordButtonHeldForTheChordWait_RunsTheMacro()
    {
        _harness.BindChord(A | X, NoAction.Instance).Begin();
        _harness.Hold(A, TimeSpan.Zero);
        _harness.Hold(A, _harness.Profile.ChordWait - OneTick);
        Assert.Empty(_harness.Sink.KeyCalls);

        _harness.Hold(A, OneTick);

        Assert.Equal([PressCall(Ctrl, C)], _harness.Sink.KeyCalls);
    }

    [Fact]
    public void Update_MacroOnChordButtonWhoseChordCompletes_DoesNotRun()
    {
        _harness.BindChord(A | X, NoAction.Instance).Begin();
        _harness.Hold(A, TimeSpan.Zero);

        _harness.Hold(A | X, TimeSpan.Zero);
        _harness.HoldFor(None, LongerThanTheMacro);

        Assert.Empty(_harness.Sink.KeyCalls);
    }
}
