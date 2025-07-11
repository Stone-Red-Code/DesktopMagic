﻿using DesktopMagic.Api;
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
    private Color oldColor = Color.White;
    private string oldFont = string.Empty;
    private bool oldDisplaySecondsCheckBoxValue;
    public override int UpdateInterval => 1000;

    public override Bitmap? Main()
    {
        string time = displaySecondsCheckBox.Value ? DateTime.Now.ToLongTimeString() : DateTime.Now.ToShortTimeString();

        if (oldTime == time && oldColor == Application.Theme.PrimaryColor && oldFont == Application.Theme.Font && oldDisplaySecondsCheckBoxValue == displaySecondsCheckBox.Value)
        {
            return null;
        }

        oldTime = time;
        oldColor = Application.Theme.PrimaryColor;
        oldFont = Application.Theme.Font;
        oldDisplaySecondsCheckBoxValue = displaySecondsCheckBox.Value;

        Font font = new Font(Application.Theme.Font, 200);

        Bitmap bmp = new Bitmap(1, 1);
        bmp.SetResolution(100, 100);
        using Graphics tmpGr = Graphics.FromImage(bmp);
        tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

        SizeF size = tmpGr.MeasureString(time, font);

        bmp = new Bitmap((int)size.Width, (int)size.Height);
        bmp.SetResolution(100, 100);

        using Graphics gr = Graphics.FromImage(bmp);

        gr.TextRenderingHint = TextRenderingHint.AntiAlias;
        gr.DrawString(time, font, new SolidBrush(Application.Theme.PrimaryColor), 0, 0);

        return bmp;
    }
}