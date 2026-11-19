namespace ControllerPC.Core.Controller;

/// <summary>
/// A digital controller button. Values for the XInput buttons match the XInput
/// <c>wButtons</c> bit layout; <see cref="LeftTrigger"/> and <see cref="RightTrigger"/>
/// are the analog triggers treated as buttons once pressed past a threshold.
/// </summary>
[Flags]
public enum ControllerButton : uint
{
    None = 0,
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000,
    LeftTrigger = 0x1_0000,
    RightTrigger = 0x2_0000,
}
