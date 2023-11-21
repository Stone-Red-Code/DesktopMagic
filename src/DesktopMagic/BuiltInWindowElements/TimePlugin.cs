using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using DesktopMagicPluginAPI.Settings;

using System;
using System.Drawing;
using System.Drawing.Text;

namespace DesktopMagic.BuiltInWindowElements;

internal class TimePlugin : Plugin
{
    [Setting("Display Seconds")]
    private readonly CheckBox displaySecondsCheckBox = new CheckBox(true);

    public override int UpdateInterval => 1000;

    private string? oldFont = null;
    private float maxWidth = 0;
    public override Bitmap Main()
    {
        string time = displaySecondsCheckBox.Value ? DateTime.Now.ToLongTimeString() : DateTime.Now.ToShortTimeString();

        Font font = new Font(Application.Theme.Font, 200);

        Bitmap bmp = new Bitmap(1, 1);
        bmp.SetResolution(100, 100);
        using Graphics tmpGr = Graphics.FromImage(bmp);
        tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

        SizeF size = tmpGr.MeasureString(time, font);

        if (oldFont != Application.Theme.Font)
        {
            oldFont = Application.Theme.Font;
            maxWidth = 0;
        }

        if (size.Width > maxWidth)
        {
            maxWidth = size.Width;
        }

        bmp = new Bitmap((int)maxWidth, (int)size.Height);
        bmp.SetResolution(100, 100);

        using Graphics gr = Graphics.FromImage(bmp);

        gr.TextRenderingHint = TextRenderingHint.AntiAlias;
        gr.DrawString(time, font, new SolidBrush(Application.Theme.PrimaryColor), 0, 0);

        return bmp;
    }
}