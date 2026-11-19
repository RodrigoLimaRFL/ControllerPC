using System.Security.Principal;

namespace ControllerPC.App.UI;

/// <summary>Tells whether this process runs with administrator rights.</summary>
public static class ProcessElevation
{
    /// <summary>
    /// True when the process token is in the Administrators role, i.e. elevated under UAC.
    /// Without elevation, synthetic input cannot reach elevated windows.
    /// </summary>
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
