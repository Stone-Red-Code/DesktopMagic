using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DesktopMagic.Helpers;

internal static partial class MultiColorConverter
{
    public static string ConvertToHex(System.Drawing.Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static string ConvertToHex(System.Windows.Media.Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static bool TryConvertToSystemColor(string hex, out System.Drawing.Color color)
    {
        hex = hex.Replace("#", "");

        if (hex.Length == 8 && Hex8().IsMatch(hex))
        {
            int a = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int r = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            color = System.Drawing.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
            return true;
        }
        else if (hex.Length == 6 && Hex6().IsMatch(hex))
        {
            int a = 255;
            int r = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            color = System.Drawing.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
            return true;
        }
        else
        {
            color = System.Drawing.Color.Transparent;
            return false;
        }
    }

    public static bool TryConvertToMediaColor(string hex, out System.Windows.Media.Color color)
    {
        hex = hex.Replace("#", "");

        if (hex.Length == 8 && Hex8().IsMatch(hex))
        {
            int a = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int r = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            color = System.Windows.Media.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
            return true;
        }
        else if (hex.Length == 6 && Hex6().IsMatch(hex))
        {
            int a = 255;
            int r = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            color = System.Windows.Media.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
            return true;
        }
        else
        {
            color = System.Windows.Media.Colors.Transparent;
            return false;
        }
    }

    public static System.Windows.Media.Color ConvertToMediaColor(System.Drawing.Color color)
    {
        return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static System.Drawing.Color ConvertToSystemColor(System.Windows.Media.Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    [GeneratedRegex("(?:[0-9a-fA-F]{8})")]
    private static partial Regex Hex8();

    [GeneratedRegex("(?:[0-9a-fA-F]{6})")]
    private static partial Regex Hex6();
}