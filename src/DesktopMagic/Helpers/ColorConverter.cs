using System.Globalization;
using System.Text.RegularExpressions;

namespace DesktopMagic.Helpers
{
    internal static class MultiColorConverter
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

            if (hex.Length == 8 && Regex.IsMatch(hex, "(?:[0-9a-fA-F]{8})"))
            {
                int a = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int r = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int g = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                color = System.Drawing.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                return true;
            }
            else if (hex.Length == 6 && Regex.IsMatch(hex, "(?:[0-9a-fA-F]{6})"))
            {
                int a = 255;
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
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

            if (hex.Length == 8 && Regex.IsMatch(hex, "(?:[0-9a-fA-F]{8})"))
            {
                int a = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int r = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int g = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                color = System.Windows.Media.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                return true;
            }
            else if (hex.Length == 6 && Regex.IsMatch(hex, "(?:[0-9a-fA-F]{6})"))
            {
                int a = 255;
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                color = System.Windows.Media.Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                return true;
            }
            else
            {
                color = System.Windows.Media.Colors.Transparent;
                return false;
            }
        }
    }
}