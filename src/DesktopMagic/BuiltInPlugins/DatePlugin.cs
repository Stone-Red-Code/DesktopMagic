using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System;
using System.Drawing;
using System.Drawing.Text;

namespace DesktopMagic.BuiltInPlugins;

internal class DatePlugin : Plugin
{
    [Setting("short-date", "Short date")]
    private readonly CheckBox shortDateCheckBox = new CheckBox(true);

    private DateTime oldDateTime = DateTime.MinValue;
    private Color oldColor = Color.White;
    private string oldFont = string.Empty;
    private bool oldShortDateCheckBoxValue;
    public override int UpdateInterval => 1000;

    public override Bitmap? Main()
    {
        if (oldDateTime.Date == DateTime.Now.Date && oldColor == Application.Theme.PrimaryColor && oldFont == Application.Theme.Font && oldShortDateCheckBoxValue == shortDateCheckBox.Value)
        {
            return null;
        }

        oldDateTime = DateTime.Now;
        oldColor = Application.Theme.PrimaryColor;
        oldFont = Application.Theme.Font;
        oldShortDateCheckBoxValue = shortDateCheckBox.Value;

        string date = shortDateCheckBox.Value ? DateTime.Now.ToShortDateString() : DateTime.Now.ToLongDateString();

        Font font = new Font(Application.Theme.Font, 200);

        Bitmap bmp = new Bitmap(1, 1);
        bmp.SetResolution(100, 100);
        using Graphics tmpGr = Graphics.FromImage(bmp);
        tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

        SizeF size = tmpGr.MeasureString(date, font);

        bmp = new Bitmap((int)size.Width, (int)size.Height);
        bmp.SetResolution(100, 100);

        using Graphics gr = Graphics.FromImage(bmp);

        gr.TextRenderingHint = TextRenderingHint.AntiAlias;
        gr.DrawString(date, font, new SolidBrush(Application.Theme.PrimaryColor), 0, 0);

        return bmp;
    }
}