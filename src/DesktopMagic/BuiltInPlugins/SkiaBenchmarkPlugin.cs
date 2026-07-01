using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using SkiaSharp;

using System;

namespace DesktopMagic.BuiltInPlugins;

internal class SkiaBenchmarkPlugin : SkiaPlugin
{
    private readonly Random rng = new(42);
    private readonly object rngLock = new();

    private readonly System.Diagnostics.Stopwatch frameTimer = System.Diagnostics.Stopwatch.StartNew();
    private double fps;
    private long lastFrameMs;

    [Setting("skia-shape-count", "Shapes")]
    private readonly IntegerUpDown shapeCount = new(10, 10000, 1000);

    [Setting("skia-shape-type", "Shape Type")]
    private readonly ComboBox shapeType = new("Rectangles", "Ellipses", "Lines", "Text", "Mixed");

    public override int UpdateInterval => 16;

    public override void Main(SKCanvas canvas)
    {
        long now = frameTimer.ElapsedMilliseconds;
        if (lastFrameMs > 0)
        {
            double elapsed = now - lastFrameMs;
            fps = fps * 0.9 + (1000.0 / Math.Max(elapsed, 1)) * 0.1;
        }
        lastFrameMs = now;

        int w = (int)canvas.DeviceClipBounds.Width;
        int h = (int)canvas.DeviceClipBounds.Height;

        canvas.Clear(SKColors.Transparent);

        SKColor color = new(
            Application.Theme.PrimaryColor.R,
            Application.Theme.PrimaryColor.G,
            Application.Theme.PrimaryColor.B,
            Application.Theme.PrimaryColor.A);

        int count = shapeCount.Value;
        string type = shapeType.Value;

        for (int i = 0; i < count; i++)
        {
            int x, y, rw, rh;
            lock (rngLock)
            {
                x = rng.Next(0, w);
                y = rng.Next(0, h);
                rw = rng.Next(5, 60);
                rh = rng.Next(5, 60);
            }

            byte alpha = (byte)rng.Next(100, 255);
            using var paint = new SKPaint
            {
                Color = color.WithAlpha(alpha),
                IsAntialias = false,
                Style = SKPaintStyle.Fill
            };

            switch (type)
            {
                case "Rectangles":
                    canvas.DrawRect(x, y, rw, rh, paint);
                    break;
                case "Ellipses":
                    canvas.DrawOval(x + rw / 2f, y + rh / 2f, rw / 2f, rh / 2f, paint);
                    break;
                case "Lines":
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 2;
                    canvas.DrawLine(x, y, x + rw, y + rh, paint);
                    break;
                case "Text":
                    using (var textFont = new SKFont(SKTypeface.Default, 16))
                    {
                        canvas.DrawText("Mg", x, y + 16, SKTextAlign.Left, textFont, paint);
                    }
                    break;
                default:
                    int shape;
                    lock (rngLock) { shape = rng.Next(0, 4); }
                    if (shape == 0)
                    {
                        canvas.DrawRect(x, y, rw, rh, paint);
                    }
                    else if (shape == 1)
                    {
                        canvas.DrawOval(x + rw / 2f, y + rh / 2f, rw / 2f, rh / 2f, paint);
                    }
                    else if (shape == 2)
                    {
                        paint.Style = SKPaintStyle.Stroke;
                        paint.StrokeWidth = 2;
                        canvas.DrawLine(x, y, x + rw, y + rh, paint);
                    }
                    else
                    {
                        using var textFont2 = new SKFont(SKTypeface.Default, 16);
                        canvas.DrawText("Mg", x, y + 16, SKTextAlign.Left, textFont2, paint);
                    }
                    break;
            }
        }

        using var fpsFont = new SKFont(SKTypeface.Default, 14);
        using var fpsPaint = new SKPaint
        {
            Color = color,
            IsAntialias = false
        };
        canvas.DrawText($"FPS: {fps:F1} | Shapes: {count} | Type: {type}", 8, 18, SKTextAlign.Left, fpsFont, fpsPaint);
    }
}
