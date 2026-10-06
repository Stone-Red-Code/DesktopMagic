using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Point = System.Windows.Point;

namespace DesktopMagic.Helpers;

/// <summary>
/// Renders the logo image used for layouts and themes on mod.io (mod.io requires one, at least 512x288).
/// </summary>
internal static class SharePreviewRenderer
{
    private const int Width = 1280;
    private const int Height = 720;

    private static readonly Color canvasColor = Color.FromRgb(0x1C, 0x1C, 0x24);
    private static readonly Color screenColor = Color.FromRgb(0x2E, 0x2E, 0x3A);

    public static void Render(IModIoShareable item, string path)
    {
        DrawingVisual visual = new();

        using (DrawingContext context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(canvasColor), null, new Rect(0, 0, Width, Height));

            switch (item)
            {
                case Layout layout:
                    DrawLayout(context, layout);
                    break;

                case Theme theme:
                    DrawTheme(context, theme);
                    break;
            }
        }

        RenderTargetBitmap bitmap = new(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void DrawLayout(DrawingContext context, Layout layout)
    {
        const double padding = 60;
        double aspectRatio = layout.ScreenAspectRatio > 0 ? layout.ScreenAspectRatio : 16.0 / 9.0;

        double availableWidth = Width - (padding * 2);
        double availableHeight = Height - (padding * 2);
        double screenWidth = Math.Min(availableWidth, availableHeight * aspectRatio);
        double screenHeight = screenWidth / aspectRatio;

        Rect screen = new((Width - screenWidth) / 2, (Height - screenHeight) / 2, screenWidth, screenHeight);
        context.DrawRoundedRectangle(new SolidColorBrush(screenColor), null, screen, 12, 12);

        foreach ((uint pluginId, PluginSettings pluginSettings) in layout.Plugins.Where(kvp => kvp.Value.Enabled))
        {
            Theme theme = pluginSettings.Theme;

            Rect widget = new(
                screen.X + (pluginSettings.Position.X * screen.Width),
                screen.Y + (pluginSettings.Position.Y * screen.Height),
                Math.Max(4, pluginSettings.Size.X * screen.Width),
                Math.Max(4, pluginSettings.Size.Y * screen.Height));
            widget.Intersect(screen);

            if (widget.IsEmpty)
            {
                continue;
            }

            Color background = MultiColorConverter.ConvertToMediaColor(theme.BackgroundColor);
            if (background.A < 0x30)
            {
                background = Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF);
            }

            Color primary = MultiColorConverter.ConvertToMediaColor(theme.PrimaryColor);
            double radius = Math.Min(theme.CornerRadius * screen.Width / 1920, Math.Min(widget.Width, widget.Height) / 2);

            context.DrawRoundedRectangle(new SolidColorBrush(background), new Pen(new SolidColorBrush(primary), 2), widget, radius, radius);

            string name = Manager.Instance.Plugins.TryGetValue(pluginId, out InternalPluginData? pluginData) ? pluginData.Metadata.Name : pluginSettings.Metadata.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            FormattedText text = CreateText(name, theme.Font, Math.Clamp(widget.Height / 4, 10, 28), primary);
            text.MaxTextWidth = Math.Max(1, widget.Width - 12);
            text.MaxTextHeight = Math.Max(1, widget.Height - 8);
            text.Trimming = TextTrimming.CharacterEllipsis;

            context.DrawText(text, new Point(widget.X + 6, widget.Y + ((widget.Height - Math.Min(text.Height, text.MaxTextHeight)) / 2)));
        }
    }

    private static void DrawTheme(DrawingContext context, Theme theme)
    {
        Color background = MultiColorConverter.ConvertToMediaColor(theme.BackgroundColor);
        Color primary = MultiColorConverter.ConvertToMediaColor(theme.PrimaryColor);
        Color secondary = MultiColorConverter.ConvertToMediaColor(theme.SecondaryColor);

        Rect card = new(120, 100, Width - 240, Height - 200);
        double radius = Math.Clamp(theme.CornerRadius * 2, 0, card.Height / 2);

        context.DrawRoundedRectangle(new SolidColorBrush(screenColor), null, card, radius, radius);
        context.DrawRoundedRectangle(new SolidColorBrush(background), null, card, radius, radius);

        FormattedText title = CreateText(theme.Name, theme.Font, 96, primary);
        title.MaxTextWidth = card.Width - 120;
        title.MaxLineCount = 1;
        title.Trimming = TextTrimming.CharacterEllipsis;
        context.DrawText(title, new Point(card.X + 60, card.Y + 60));

        FormattedText subtitle = CreateText(theme.Font, theme.Font, 48, secondary);
        subtitle.MaxTextWidth = card.Width - 120;
        subtitle.MaxLineCount = 1;
        subtitle.Trimming = TextTrimming.CharacterEllipsis;
        context.DrawText(subtitle, new Point(card.X + 60, card.Y + 60 + title.Height + 10));

        Color[] swatches = [primary, secondary, background];
        for (int i = 0; i < swatches.Length; i++)
        {
            Point center = new(card.X + 110 + (i * 130), card.Bottom - 110);
            context.DrawEllipse(new SolidColorBrush(swatches[i]), new Pen(new SolidColorBrush(Color.FromArgb(0x60, 0x80, 0x80, 0x80)), 3), center, 50, 50);
        }
    }

    private static FormattedText CreateText(string text, string font, double size, Color color)
    {
        return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(font), size, new SolidColorBrush(color), 1.0);
    }
}
