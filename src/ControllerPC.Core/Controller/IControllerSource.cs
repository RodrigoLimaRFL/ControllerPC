namespace ControllerPC.Core.Controller;

/// <summary>Reads the current state of connected controllers.</summary>
public interface IControllerSource
{
    /// <summary>Number of controller slots (XInput supports 4).</summary>
    int SlotCount { get; }

    /// <summary>Reads the controller in <paramref name="slot"/>; <c>null</c> when nothing is connected there.</summary>
    ControllerState? Read(int slot);
}
