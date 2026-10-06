using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using SkiaSharp;

using System;

namespace DesktopMagic.BuiltInPlugins;

internal class TimePlugin : SkiaPlugin
{
    private const float FontSize = 200;
    private const float ContentPadding = 8;

    [Setting("display-seconds", "Show Seconds")]
    private readonly CheckBox displaySecondsCheckBox = new CheckBox(true);

    public override int UpdateInterval => 1000;

    public override SKImage? Main()
    {
        string time = GetTime();

        using SKTypeface typeface = SKTypeface.FromFamilyName(Application.Theme.Font);
        using SKFont font = new(typeface, FontSize);
        using SKPaint paint = new()
        {
            Color = new SKColor(
                Application.Theme.PrimaryColor.R,
                Application.Theme.PrimaryColor.G,
                Application.Theme.PrimaryColor.B,
                Application.Theme.PrimaryColor.A),
            IsAntialias = true
        };

        float textWidth = font.MeasureText(time, paint);
        SKFontMetrics metrics = font.Metrics;
        float textHeight = metrics.Descent - metrics.Ascent;

        int width = (int)Math.Ceiling(textWidth) + (int)(ContentPadding * 2);
        int height = (int)Math.Ceiling(textHeight) + (int)(ContentPadding * 2);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);

        // Draw the text at a fixed size into a content-sized image; the window scales it to fit.
        surface.Canvas.DrawText(time, ContentPadding, -metrics.Ascent + ContentPadding, SKTextAlign.Left, font, paint);

        return surface.Snapshot();
    }

    private string GetTime()
    {
        return displaySecondsCheckBox.Value ? DateTime.Now.ToLongTimeString() : DateTime.Now.ToShortTimeString();
    }
}
