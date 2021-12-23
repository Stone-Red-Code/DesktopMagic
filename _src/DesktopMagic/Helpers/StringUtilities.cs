using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic.Helpers
{
    internal static class StringUtilities
    {
        public static Size MeasureString(string input, TextBlock element)
        {
            FormattedText formattedText = new FormattedText(
                input,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(element.FontFamily, element.FontStyle, element.FontWeight, element.FontStretch),
                element.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}