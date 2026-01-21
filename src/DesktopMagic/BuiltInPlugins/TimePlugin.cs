using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System;
using System.Drawing;
using System.Drawing.Text;

namespace DesktopMagic.BuiltInPlugins;

internal class TimePlugin : Plugin
{
    [Setting("display-seconds", "Show Seconds")]
    private readonly CheckBox displaySecondsCheckBox = new CheckBox(true);

    private string oldTime = string.Empty;
    private bool themeChanged;

    public override int UpdateInterval => 1000;

    public override Bitmap? Main()
    {
        string time = displaySecondsCheckBox.Value ? DateTime.Now.ToLongTimeString() : DateTime.Now.ToShortTimeString();

        if (oldTime == time && !themeChanged)
        {
            return null;
        }

        oldTime = time;
        themeChanged = false;

        using Font font = new Font(Application.Theme.Font, 200);

        Bitmap bmp = new Bitmap(1, 1);
        bmp.SetResolution(100, 100);
        using Graphics tmpGr = Graphics.FromImage(bmp);
        tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

        SizeF size = tmpGr.MeasureString(time, font);

        bmp = new Bitmap((int)size.Width, (int)size.Height);
        bmp.SetResolution(100, 100);

        using Graphics gr = Graphics.FromImage(bmp);
        using SolidBrush brush = new SolidBrush(Application.Theme.PrimaryColor);

        gr.TextRenderingHint = TextRenderingHint.AntiAlias;
        gr.DrawString(time, font, brush, 0, 0);

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
}