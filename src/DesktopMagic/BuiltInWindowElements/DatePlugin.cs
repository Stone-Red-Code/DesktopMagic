using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;

using System;
using System.Drawing;
using System.Drawing.Text;

namespace DesktopMagic.BuiltInWindowElements;

internal class DatePlugin : Plugin
{
    [Element("Short date:")]
    private readonly CheckBox shortDatecheckBox = new CheckBox(true);

    private DateTime oldDateTime = DateTime.MinValue;
    private Color oldColor = Color.White;
    private string oldFont;
    private bool oldShortDatecheckBoxValue;
    public override int UpdateInterval => 1000;

    public override Bitmap Main()
    {
        if (oldDateTime.Date == DateTime.Now.Date && oldColor == Application.Theme.PrimaryColor && oldFont == Application.Theme.Font && oldShortDatecheckBoxValue == shortDatecheckBox.Value)
        {
            return null;
        }

        oldDateTime = DateTime.Now;
        oldColor = Application.Theme.PrimaryColor;
        oldFont = Application.Theme.Font;
        oldShortDatecheckBoxValue = shortDatecheckBox.Value;

        string date = shortDatecheckBox.Value ? DateTime.Now.ToShortDateString() : DateTime.Now.ToLongDateString();

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