using System.Globalization;
using Evergine.Common.Graphics;

namespace Redpoint.SceneViewer.Utilities;

public static class ColorHelper
{
    public static Color TryParseColor(string hex, Color fallback)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Remove(0, 1);
        }
        
        if (!string.IsNullOrWhiteSpace(hex) && hex.Length >= 8 &&
            byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out var a) &&
            byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, null, out var b))
        {
            return new Color(r, g, b, a);
        }
        
        if (!string.IsNullOrWhiteSpace(hex) && hex.Length >= 6 &&
            byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out r) &&
            byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out g) &&
            byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out b))
        {
            return new Color(r, g, b, 255);
        }

        return fallback;
    }
}