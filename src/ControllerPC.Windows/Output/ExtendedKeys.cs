namespace ControllerPC.Windows.Output;

/// <summary>
/// Virtual keys whose scan code carries the 0xE0 prefix, so they must be sent with <c>KEYEVENTF_EXTENDEDKEY</c>.
/// Without the flag Windows sees the numeric-keypad twin (e.g. Left Arrow becomes Numpad 4 with Num Lock off) or
/// the left-hand modifier.
/// </summary>
internal static class ExtendedKeys
{
    private const ushort Cancel = 0x03;
    private const ushort PageUp = 0x21;
    private const ushort PageDown = 0x22;
    private const ushort End = 0x23;
    private const ushort Home = 0x24;
    private const ushort LeftArrow = 0x25;
    private const ushort UpArrow = 0x26;
    private const ushort RightArrow = 0x27;
    private const ushort DownArrow = 0x28;
    private const ushort PrintScreen = 0x2C;
    private const ushort Insert = 0x2D;
    private const ushort Delete = 0x2E;
    private const ushort LeftWindows = 0x5B;
    private const ushort RightWindows = 0x5C;
    private const ushort Applications = 0x5D;
    private const ushort NumpadDivide = 0x6F;
    private const ushort NumLock = 0x90;
    private const ushort RightControl = 0xA3;
    private const ushort RightAlt = 0xA5;

    /// <summary><c>VK_BROWSER_BACK</c>: first of the browser, volume, media and launch keys (all extended).</summary>
    private const ushort FirstBrowserOrMediaKey = 0xA6;

    /// <summary><c>VK_LAUNCH_APP2</c>: last of the browser, volume, media and launch keys.</summary>
    private const ushort LastBrowserOrMediaKey = 0xB7;

    public static bool Contains(ushort virtualKey) =>
        virtualKey is Cancel
            or PageUp or PageDown or End or Home
            or LeftArrow or UpArrow or RightArrow or DownArrow
            or PrintScreen or Insert or Delete
            or LeftWindows or RightWindows or Applications
            or NumpadDivide or NumLock
            or RightControl or RightAlt
            or (>= FirstBrowserOrMediaKey and <= LastBrowserOrMediaKey);
}
