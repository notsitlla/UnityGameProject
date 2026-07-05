using UnityEngine;

public static class ThemePalette
{
    public static Color Background = ColorFromHex("#121623");
    public static Color Panel = ColorFromHex("#1E2538");
    public static Color CyanGlow = ColorFromHex("#32D8FF");
    public static Color Magenta = ColorFromHex("#FF4FD8");
    public static Color Gold = ColorFromHex("#FFD54A");
    public static Color White = ColorFromHex("#F3F7FF");

    // Helper method to convert Hex strings to Unity Color objects
    private static Color ColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        return Color.white;
    }
}