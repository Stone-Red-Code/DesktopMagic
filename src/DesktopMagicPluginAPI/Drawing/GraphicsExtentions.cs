using System;
using System.Collections.Generic;
using System.Drawing;

namespace DesktopMagic.Api.Drawing;

/// <summary>
/// Extensions for the <see cref="Graphics"/> class.
/// </summary>
public static class GraphicsExtentions
{
    private static readonly Dictionary<Font, int> fonts = new Dictionary<Font, int>(new FontComparer());

    /// <summary>
    /// <inheritdoc cref="Graphics.DrawString(string?, Font, Brush, float, float)"/>
    /// </summary>
    /// <param name="graphics">Graphics object.</param>
    /// <param name="s">String to draw.</param>
    /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
    /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
    /// <param name="x">The x-coordinate of the upper-left corner of the drawn text.</param>
    /// <param name="y">The y-coordinate of the upper-left corner of the drawn text.</param>
    public static void DrawStringMonospace(this Graphics graphics, string s, Font font, Brush brush, float x, float y)
    {
        int widest = graphics.GetWidestChar(font);

        for (int i = 0; i < s.Length; i++)
        {
            graphics.DrawString(s[i].ToString(), font, brush, x, y);

            x += widest;
        }
    }

    /// <summary>
    /// <inheritdoc cref="Graphics.DrawString(string?, Font, Brush, PointF)"/>
    /// </summary>
    /// <param name="graphics">Graphics object.</param>
    /// <param name="s">String to draw.</param>
    /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
    /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
    /// <param name="point"><see cref="PointF"/> structure that specifies the upper-left corner of the drawn text.</param>
    public static void DrawStringMonospace(this Graphics graphics, string s, Font font, Brush brush, PointF point)
    {
        int widest = graphics.GetWidestChar(font);

        for (int i = 0; i < s.Length; i++)
        {
            graphics.DrawString(s[i].ToString(), font, brush, point);

            point.X += widest;
        }
    }

    /// <summary>
    /// <inheritdoc cref="Graphics.DrawString(string?, Font, Brush, float, float)"/>
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
    /// <inheritdoc cref="Graphics.DrawString(string?, Font, Brush, PointF)"/>
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

    /// <summary>
    ///<inheritdoc cref="Graphics.DrawString(string?, Font, Brush, float, float)"/>
    /// </summary>
    /// <param name="graphics">Graphics object.</param>
    /// <param name="s">String to draw.</param>
    /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
    /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
    /// <param name="x">The x-coordinate of the upper-left corner of the drawn text.</param>
    /// <param name="y">The y-coordinate of the upper-left corner of the drawn text.</param>
    public static void DrawStringNoLeftPadding(this Graphics graphics, string s, Font font, Brush brush, float x, float y)
    {
        // measure left padding
        StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
        sf.SetMeasurableCharacterRanges([new CharacterRange(0, 1)]);
        Region[] r = graphics.MeasureCharacterRanges(s, font, new RectangleF(0, 0, 1000, 100), sf);
        float leftPadding = r[0].GetBounds(graphics).Left;

        // draw string
        sf = new StringFormat(StringFormatFlags.NoClip);
        graphics.DrawString(s, font, brush, x - leftPadding, y, sf);
    }

    /// <summary>
    /// <inheritdoc cref="Graphics.DrawString(string?, Font, Brush, PointF)"/>
    /// </summary>
    /// <param name="graphics">Graphics object.</param>
    /// <param name="s">String to draw.</param>
    /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
    /// <param name="brush"><see cref="Brush"/> that determines the color and texture of the drawn text.</param>
    /// <param name="point"><see cref="PointF"/> structure that specifies the upper-left corner of the drawn text.</param>
    public static void DrawStringNoLeftPadding(this Graphics graphics, string s, Font font, Brush brush, PointF point)
    {
        // measure left padding
        StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
        sf.SetMeasurableCharacterRanges([new CharacterRange(0, 1)]);
        Region[] r = graphics.MeasureCharacterRanges(s, font, new RectangleF(0, 0, 1000, 100), sf);
        float leftPadding = r[0].GetBounds(graphics).Left;

        // draw string
        sf = new StringFormat(StringFormatFlags.NoClip);
        graphics.DrawString(s, font, brush, point.X - leftPadding, point.Y, sf);
    }

    /// <summary>
    /// <inheritdoc cref="Graphics.MeasureString(string, Font)"/>
    /// </summary>
    /// <param name="graphics">Graphics object.</param>
    /// <param name="text">String to measure.</param>
    /// <param name="font"><see cref="Font"/> that defines the text format of the string.</param>
    /// <returns></returns>
    public static SizeF MeasureStringNoLeftPadding(this Graphics graphics, string text, Font font)
    {
        SizeF size = graphics.MeasureString(text, font, int.MaxValue);

        StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
        sf.SetMeasurableCharacterRanges([new CharacterRange(0, 1)]);
        Region[] r = graphics.MeasureCharacterRanges(text, font, new RectangleF(0, 0, 1000, 100), sf);
        float leftPadding = r[0].GetBounds(graphics).Left;

        size.Width -= leftPadding / 1.5f;
        return size;
    }

    private static int GetWidestChar(this Graphics graphics, Font font)
    {
        if (fonts.TryGetValue(font, out int value))
        {
            return value;
        }

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