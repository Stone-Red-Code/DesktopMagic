using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DesktopMagic.BuiltInPlugins;

internal class BenchmarkPlugin : Plugin
{
    private const int BitmapWidth = 400;
    private const int BitmapHeight = 300;

    private readonly Random rng = new(42);
    private readonly object rngLock = new();

    private readonly System.Diagnostics.Stopwatch frameTimer = System.Diagnostics.Stopwatch.StartNew();
    private double fps;
    private long lastFrameMs;

    [Setting("shape-count", "Shapes")]
    private readonly IntegerUpDown shapeCount = new(10, 10000, 1000);

    [Setting("shape-type", "Shape Type")]
    private readonly ComboBox shapeType = new("Rectangles", "Ellipses", "Lines", "Text", "Mixed");

    [Setting("fps-counter", "Fps")]
    private readonly Label fpsCounter = new("");

    public override int UpdateInterval => 16;

    public override Bitmap? Main()
    {
        long now = frameTimer.ElapsedMilliseconds;
        if (lastFrameMs > 0)
        {
            double elapsed = now - lastFrameMs;
            fps = (fps * 0.9) + (1000.0 / Math.Max(elapsed, 1) * 0.1);
        }
        lastFrameMs = now;

        int w = BitmapWidth;
        int h = BitmapHeight;

        Bitmap bmp = new Bitmap(w, h);
        using Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.SmoothingMode = SmoothingMode.HighSpeed;
        g.CompositingQuality = CompositingQuality.HighSpeed;
        g.InterpolationMode = InterpolationMode.Low;

        Color color = Application.Theme.PrimaryColor;
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

            using Brush brush = new SolidBrush(Color.FromArgb(rng.Next(100, 255), color));

            switch (type)
            {
                case "Rectangles":
                    g.FillRectangle(brush, x, y, rw, rh);
                    break;
                case "Ellipses":
                    g.FillEllipse(brush, x, y, rw, rh);
                    break;
                case "Lines":
                    {
                        int thickness;
                        lock (rngLock) { thickness = rng.Next(1, 5); }
                        g.DrawLine(new Pen(brush, thickness), x, y, x + rw, y + rh);
                    }
                    break;
                case "Text":
                    {
                        int fontSize;
                        lock (rngLock) { fontSize = rng.Next(8, 24); }
                        using Font font = new("Arial", fontSize);
                        g.DrawString("Mg", font, brush, x, y);
                    }
                    break;
                default:
                    {
                        int shape, thickness, fontSize;
                        lock (rngLock)
                        {
                            shape = rng.Next(0, 4);
                            thickness = rng.Next(1, 5);
                            fontSize = rng.Next(8, 24);
                        }
                        if (shape == 0)
                        {
                            g.FillRectangle(brush, x, y, rw, rh);
                        }
                        else if (shape == 1)
                        {
                            g.FillEllipse(brush, x, y, rw, rh);
                        }
                        else if (shape == 2)
                        {
                            g.DrawLine(new Pen(brush, thickness), x, y, x + rw, y + rh);
                        }
                        else
                        {
                            using Font font = new("Arial", fontSize);
                            g.DrawString("Mg", font, brush, x, y);
                        }
                    }
                    break;
            }
        }

        fpsCounter.Value = fps.ToString();

        return bmp;
    }
}
