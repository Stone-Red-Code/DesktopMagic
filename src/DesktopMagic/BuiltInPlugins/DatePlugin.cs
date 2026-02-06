using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopMagic.BuiltInPlugins;

internal partial class DatePlugin : Plugin
{
    [Setting("week-day-style", "Week Day Style")]
    public ComboBox weekDayStyleComboBox = new ComboBox([.. Enum.GetValues<WeekdayStyle>().Select(e => e.ToString())]);

    [Setting("day-style", "Day Style")]
    public ComboBox dayStyleComboBox = new ComboBox([.. Enum.GetValues<DayStyle>().Select(e => e.ToString())]);

    [Setting("month-style", "Month Style")]
    public ComboBox monthStyleComboBox = new ComboBox([.. Enum.GetValues<MonthStyle>().Select(e => e.ToString())]);

    [Setting("year-style", "Year Style")]
    public ComboBox yearStyleComboBox = new ComboBox([.. Enum.GetValues<YearStyle>().Select(e => e.ToString())]);

    [Setting("use-date-separators", "Use Date Separators")]
    public CheckBox useDateSeparators = new CheckBox(true);

    [Setting("use-color-override-for-weekend", "Use Color Override For Weekend")]
    public CheckBox useColorOverrideForWeekend = new CheckBox(false);

    [Setting("weekend-color-override", "Weekend Color Override")]
    public ColorPicker weekendColorOverride = new ColorPicker(Color.Red);

    private DateTime oldDateTime = DateTime.MinValue;
    private bool themeChanged = false;

    public override int UpdateInterval => 1000;

    public override Bitmap? Main()
    {
        if (oldDateTime.Date == DateTime.Now.Date && !themeChanged)
        {
            return null;
        }

        oldDateTime = DateTime.Now;
        themeChanged = false;

        CultureInfo culture = CultureInfo.CurrentCulture;
        DateTime dateTime = DateTime.Now;

        DateTimeFormatInfo dtf = culture.DateTimeFormat;

        string pattern = dtf.LongDatePattern;

        WeekdayStyle weekdayStyle = Enum.Parse<WeekdayStyle>(weekDayStyleComboBox.Value);
        DayStyle dayStyle = Enum.Parse<DayStyle>(dayStyleComboBox.Value);
        MonthStyle monthStyle = Enum.Parse<MonthStyle>(monthStyleComboBox.Value);
        YearStyle yearStyle = Enum.Parse<YearStyle>(yearStyleComboBox.Value);

        // Weekday: ddd, dddd
        pattern = WeekdayRegex().Replace(pattern, weekdayStyle switch
        {
            WeekdayStyle.Long => "dddd",
            WeekdayStyle.Short => "ddd",
            _ => ""
        });

        // Day: d, dd
        pattern = DayRegex().Replace(pattern, dayStyle switch
        {
            DayStyle.Numeric => "dd",
            _ => ""
        });

        // Month: M, MM, MMM, MMMM
        pattern = MonthRegex().Replace(pattern, monthStyle switch
        {
            MonthStyle.Long => "MMMM",
            MonthStyle.Short => "MMM",
            MonthStyle.Numeric => "MM",
            _ => ""
        });

        // Year: yy, yyyy
        pattern = YearRegex().Replace(pattern, yearStyle switch
        {
            YearStyle.FourDigit => "yyyy",
            YearStyle.TwoDigit => "yy",
            _ => ""
        });

        // Clean up leftover punctuation/spacing
        pattern = CleanupRegex().Replace(pattern, " ").Trim();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            pattern = dtf.LongDatePattern;
        }

        string date = dateTime.ToString(pattern, culture);

        if (useDateSeparators.Value)
        {
            date = DateSeparatorsRegex().Replace(date, dtf.DateSeparator);
        }

        Color color = useColorOverrideForWeekend.Value && (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
            ? weekendColorOverride.Value
            : Application.Theme.PrimaryColor;

        using Font font = new Font(Application.Theme.Font, 200);

        Bitmap bmp = new Bitmap(1, 1);
        bmp.SetResolution(100, 100);
        using Graphics tmpGr = Graphics.FromImage(bmp);
        tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

        SizeF size = tmpGr.MeasureString(date, font);

        bmp = new Bitmap((int)size.Width, (int)size.Height);
        bmp.SetResolution(100, 100);

        using Graphics gr = Graphics.FromImage(bmp);
        using SolidBrush brush = new SolidBrush(color);

        gr.TextRenderingHint = TextRenderingHint.AntiAlias;
        gr.DrawString(date, font, brush, 0, 0);

        return bmp;
    }

    public override void OnThemeChanged()
    {
        themeChanged = true;
    }

    public override void OnSettingsChanged()
    {
        themeChanged = true;
    }

    [GeneratedRegex(@"d{3,4}")]
    private static partial Regex WeekdayRegex();

    [GeneratedRegex(@"\b(?<!d)d{1,2}\b")]
    private static partial Regex DayRegex();

    [GeneratedRegex(@"M{1,4}")]
    private static partial Regex MonthRegex();

    [GeneratedRegex(@"y{2,4}")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"[\s,./\-]+")]
    private static partial Regex CleanupRegex();

    [GeneratedRegex(@"(?<=\d)\s+(?=\d)")]
    private static partial Regex DateSeparatorsRegex();

    public enum WeekdayStyle { None, Short, Long }
    public enum DayStyle { None, Numeric }
    public enum MonthStyle { None, Numeric, Short, Long }
    public enum YearStyle { None, TwoDigit, FourDigit }
}
