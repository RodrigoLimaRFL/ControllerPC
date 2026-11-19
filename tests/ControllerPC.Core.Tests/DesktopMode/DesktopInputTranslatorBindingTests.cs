using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using static ControllerPC.Core.Tests.Fakes.RecordingInputSink;

namespace ControllerPC.Core.Tests.DesktopMode;

public sealed class DesktopInputTranslatorBindingTests
{
    private const ushort Shift = 0x10;
    private const ushort Ctrl = 0x11;
    private const ushort Escape = 0x1B;
    private const ushort C = 0x43;
    private const ushort V = 0x56;

    private const ControllerButton None = ControllerButton.None;
    private const ControllerButton A = ControllerButton.A;
    private const ControllerButton B = ControllerButton.B;
    private const ControllerButton X = ControllerButton.X;
    private const ControllerButton Y = ControllerButton.Y;
    private const ControllerButton LeftShoulder = ControllerButton.LeftShoulder;

    private static readonly KeyCombination Copy = new([Ctrl, C]);
    private static readonly KeyCombination Paste = new([Ctrl, V]);

    private readonly TranslatorHarness _harness = new();

    [Fact]
    public void Update_MouseButtonBindingPressed_PressesMouseButton()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Left)).Begin();

        _harness.Hold(A);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_MouseButtonBindingHeld_PressesOnlyOnce()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Left)).Begin();

        _harness.Hold(A);
        _harness.Hold(A);
        _harness.Hold(A);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_MouseButtonBindingReleased_ReleasesMouseButton()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Right)).Begin();
        _harness.Hold(A);

        _harness.Hold(None);

        Assert.Equal(
            [MouseEvent("down", MouseButton.Right), MouseEvent("up", MouseButton.Right)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_KeyCombinationBindingPressed_PressesKeysInOrder()
    {
        _harness.Bind(A, new KeyCombinationAction(new KeyCombination([Ctrl, Shift, Escape]))).Begin();

        _harness.Hold(A);

        Assert.Equal(
            [KeyEvent("down", Ctrl), KeyEvent("down", Shift), KeyEvent("down", Escape)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_KeyCombinationBindingHeld_KeepsKeysDown()
    {
        _harness.Bind(A, new KeyCombinationAction(Copy)).Begin();

        _harness.Hold(A);
        _harness.Hold(A);

        Assert.Equal([Ctrl, C], _harness.Sink.KeysDown.Order());
        Assert.Equal(2, _harness.Sink.ButtonEvents.Count);
    }

    [Fact]
    public void Update_KeyCombinationBindingReleased_ReleasesKeysInReverseOrder()
    {
        _harness.Bind(A, new KeyCombinationAction(new KeyCombination([Ctrl, Shift, Escape]))).Begin();
        _harness.Hold(A);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(None);

        Assert.Equal(
            [KeyEvent("up", Escape), KeyEvent("up", Shift), KeyEvent("up", Ctrl)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_SecondBindingSharesHeldKey_DoesNotPressItAgain()
    {
        _harness.Bind(A, new KeyCombinationAction(Copy)).Bind(B, new KeyCombinationAction(Paste)).Begin();
        _harness.Hold(A);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(A | B);

        Assert.Equal([KeyEvent("down", V)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_OneOfTwoBindingsSharingKeyReleased_KeepsSharedKeyDown()
    {
        _harness.Bind(A, new KeyCombinationAction(Copy)).Bind(B, new KeyCombinationAction(Paste)).Begin();
        _harness.Hold(A);
        _harness.Hold(A | B);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(B);

        Assert.Equal([KeyEvent("up", C)], _harness.Sink.ButtonEvents);
        Assert.Equal([Ctrl, V], _harness.Sink.KeysDown.Order());
    }

    [Fact]
    public void Update_LastBindingSharingKeyReleased_ReleasesSharedKey()
    {
        _harness.Bind(A, new KeyCombinationAction(Copy)).Bind(B, new KeyCombinationAction(Paste)).Begin();
        _harness.Hold(A);
        _harness.Hold(A | B);
        _harness.Hold(B);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(None);

        Assert.Equal([KeyEvent("up", V), KeyEvent("up", Ctrl)], _harness.Sink.ButtonEvents);
        Assert.Empty(_harness.Sink.KeysDown);
    }

    [Fact]
    public void Update_BindingReleasedAndAnotherPressedInSamePoll_ReleasesBeforePressing()
    {
        _harness.Bind(A, new KeyCombinationAction(Copy)).Bind(B, new KeyCombinationAction(Paste)).Begin();
        _harness.Hold(A);
        _harness.Sink.ButtonEvents.Clear();

        _harness.Hold(B);

        Assert.Equal(
            [KeyEvent("up", C), KeyEvent("up", Ctrl), KeyEvent("down", Ctrl), KeyEvent("down", V)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_OneOfTwoBindingsSharingMouseButtonReleased_KeepsMouseButtonDown()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Left)).Bind(B, new MouseButtonAction(MouseButton.Left)).Begin();
        _harness.Hold(A | B);

        _harness.Hold(B);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_LastBindingSharingMouseButtonReleased_ReleasesMouseButton()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Left)).Bind(B, new MouseButtonAction(MouseButton.Left)).Begin();
        _harness.Hold(A | B);
        _harness.Hold(B);

        _harness.Hold(None);

        Assert.Equal(
            [MouseEvent("down", MouseButton.Left), MouseEvent("up", MouseButton.Left)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_BindingChangedWhileHeld_ReleasesWhatWasPressed()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Left)).Begin();
        _harness.Hold(A);
        _harness.Bind(A, new KeyCombinationAction(Copy));

        _harness.Hold(None);

        Assert.Equal(
            [MouseEvent("down", MouseButton.Left), MouseEvent("up", MouseButton.Left)],
            _harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_NoActionBindingPressed_DoesNothing()
    {
        _harness.Bind(A, NoAction.Instance).Begin();

        var profileSwitch = _harness.Hold(A);
        _harness.Hold(None);

        Assert.Null(profileSwitch);
        Assert.Empty(_harness.Sink.ButtonEvents);
        Assert.Empty(_harness.Sink.CursorMoves);
    }

    [Fact]
    public void Update_UnboundButtonPressed_DoesNothing()
    {
        _harness.Begin();

        var profileSwitch = _harness.Hold(X);
        _harness.Hold(None);

        Assert.Null(profileSwitch);
        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_ProfileSwitchBindingPressed_ReturnsItsAction()
    {
        var action = new SwitchToProfileAction(Guid.NewGuid());
        _harness.Bind(Y, action).Begin();

        Assert.Equal(action, _harness.Hold(Y));
    }

    [Fact]
    public void Update_ProfileSwitchBindingHeld_ReturnsActionOnlyOnPress()
    {
        _harness.Bind(Y, new NextProfileAction()).Begin();
        _harness.Hold(Y);

        Assert.Null(_harness.Hold(Y));
    }

    [Fact]
    public void Update_ProfileSwitchBindingPressedAgain_ReturnsActionAgain()
    {
        _harness.Bind(Y, new NextProfileAction()).Begin();
        _harness.Hold(Y);
        _harness.Hold(None);

        Assert.IsType<NextProfileAction>(_harness.Hold(Y));
    }

    [Fact]
    public void Update_ProfileSwitchBindingPressed_SendsNoInput()
    {
        _harness.Bind(Y, new NextProfileAction()).Begin();

        _harness.Hold(Y);
        _harness.Hold(None);

        Assert.Empty(_harness.Sink.ButtonEvents);
    }

    [Fact]
    public void Update_TwoProfileSwitchBindingsPressedTogether_ReturnsFirstInButtonOrder()
    {
        // LeftShoulder has the lower flag value but comes after Y in ControllerButtons.All.
        _harness.Bind(LeftShoulder, new SwitchToProfileAction(Guid.NewGuid())).Bind(Y, new NextProfileAction()).Begin();

        Assert.IsType<NextProfileAction>(_harness.Hold(LeftShoulder | Y));
    }

    [Fact]
    public void Update_ProfileSwitchPressedWithOtherBindings_StillPressesTheOthers()
    {
        _harness.Bind(A, new MouseButtonAction(MouseButton.Left)).Bind(Y, new NextProfileAction()).Begin();

        _harness.Hold(A | Y);

        Assert.Equal([MouseEvent("down", MouseButton.Left)], _harness.Sink.ButtonEvents);
    }
}
