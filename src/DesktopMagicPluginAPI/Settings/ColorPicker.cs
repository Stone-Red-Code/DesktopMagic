using System.Drawing;
using System.Globalization;

namespace DesktopMagic.Api.Settings;

/// <summary>
/// Represents a color picker control.
/// </summary>
public sealed class ColorPicker : Setting
{
    private Color _value;

    /// <summary>
    /// Gets or sets the color value assigned to the <see cref="ColorPicker"/> element.
    /// </summary>
    public Color Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                ValueChanged();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorPicker"/> class with the provided <paramref name="defaultColor"/>.
    /// </summary>
    /// <param name="defaultColor">The default color value.</param>
    public ColorPicker(Color defaultColor)
    {
        _value = defaultColor;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorPicker"/> class with a default color of White.
    /// </summary>
    public ColorPicker() : this(Color.White)
    {
    }

    internal override string GetJsonValue()
    {
        return $"{Value.A},{Value.R},{Value.G},{Value.B}";
    }

    internal override void SetJsonValue(string value)
    {
        string[] parts = value.Split(',');
        if (parts.Length == 4 &&
            byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte a) &&
            byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r) &&
            byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g) &&
            byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b))
        {
            Value = Color.FromArgb(a, r, g, b);
        }
    }
}
