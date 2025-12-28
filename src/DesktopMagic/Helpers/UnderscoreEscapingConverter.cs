using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopMagic.Helpers;

public class UnderscoreEscapingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            // Replaces single underscores with double underscores to escape them in labels
            return text.Replace("_", "__");
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
