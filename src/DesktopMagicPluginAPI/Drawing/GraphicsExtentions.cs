using System;
using System.Collections.Generic;
using System.Drawing;

namespace DesktopMagicPluginAPI.Drawing
{
    /// <summary>
    /// Extentions for the <see cref="Graphics"/> class.
    /// </summary>
    public static class GraphicsExtentions
    {
        private static readonly Dictionary<Font, int> fonts = new Dictionary<Font, int>(new FontComparer());

        /// <summary>
        /// Draws the specified text string at the specified location with the specified <see cref="Brush"/> and <see cref="Font"/> objects.
        /// </summary>
        /// <param name="graphics">Graphics object.</param>
        /// <param name="s">String to draw.</param>
        /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
        /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
        /// <param name="x">The x-coordinate of the upper-left corner of the drawn text.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the drawn text.</param>
        public static void DrawStringMonospace(this Graphics graphics, string s, Font font, Brush brush, float x, float y)
        {
            int widest = GetWidestChar(font);

            for (int i = 0; i < s.Length; i++)
            {
                graphics.DrawString(s[i].ToString(), font, brush, x, y);

                x += widest;
            }
        }

        /// <summary>
        /// Draws the specified text string at the specified location with the specified <see cref="Brush"/> and <see cref="Font"/> objects.
        /// </summary>
        /// <param name="graphics">Graphics object.</param>
        /// <param name="s">String to draw.</param>
        /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
        /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
        /// <param name="point"><see cref="PointF"/> structure that specifies the upper-left corner of the drawn text.</param>
        public static void DrawStringMonospace(this Graphics graphics, string s, Font font, Brush brush, PointF point)
        {
            int widest = GetWidestChar(font);

            for (int i = 0; i < s.Length; i++)
            {
                graphics.DrawString(s[i].ToString(), font, brush, point);

                point.X += widest;
            }
        }

        /// <summary>
        /// Draws the specified text string at the specified location with the specified <see cref="Brush"/> and <see cref="Font"/> objects.
        /// </summary>
        /// <param name="graphics">Graphics object.</param>
        /// <param name="s">String to draw.</param>
        /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
        /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
        /// <param name="x">The x-coordinate of the upper-left corner of the drawn text.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the drawn text.</param>
        /// /// <param name="width">The specified width.</param>
        public static void DrawStringFixedWidth(this Graphics graphics, string s, Font font, Brush brush, float x, float y, float width)
        {
            for (int i = 0; i < s.Length; i++)
            {
                graphics.DrawString(s[i].ToString(), font, brush, x, y);

                x += width;
            }
        }

        /// <summary>
        /// Draws the specified text string at the specified location with the specified <see cref="Brush"/> and <see cref="Font"/> objects.
        /// </summary>
        /// <param name="graphics">Graphics object.</param>
        /// <param name="s">String to draw.</param>
        /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
        /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
        /// <param name="point"><see cref="PointF"/> structure that specifies the upper-left corner of the drawn text.</param>
        /// <param name="width">The specified width.</param>
        public static void DrawStringFixedWidth(this Graphics graphics, string s, Font font, Brush brush, PointF point, float width)
        {
            for (int i = 0; i < s.Length; i++)
            {
                graphics.DrawString(s[i].ToString(), font, brush, point);

                point.X += width;
            }
        }

        private static int GetWidestChar(Font font)
        {
            if (fonts.ContainsKey(font))
                return fonts[font];

            Graphics graphics = Graphics.FromImage(new Bitmap(1, 1));
            float max = 0;
            char maxx = ' ';
            for (int i = 0; i <= 255; i++)
            {
                char c = (char)i;
                if (char.IsLetterOrDigit(c))
                {
                    float neww = graphics.MeasureString(c.ToString(), font).Width;
                    if (neww >= max)
                    {
                        max = neww;
                        maxx = c;
                    }
                }
            }
            Console.WriteLine((int)Math.Round(max, 0));
            Console.WriteLine(maxx);
            fonts.Add(font, (int)Math.Round(max, 0));
            return (int)Math.Round(max, 0);
        }
    }
}