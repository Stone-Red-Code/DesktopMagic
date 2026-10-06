using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DesktopMagic.Api.Settings;

/// <summary>
/// Represents a slider control.
/// </summary>
public sealed class Slider : Setting
{
    private double _value;

    /// <summary>
    /// Gets or sets the maximum value for the <see cref="Slider"/> element.
    /// </summary>
    public double Maximum { get; }

    /// <summary>
    /// Gets or sets the minimum value for the <see cref="Slider"/> element.
    /// </summary>
    public double Minimum { get; }

    /// <summary>
    /// Gets or sets the value assigned to the <see cref="Slider"/> element.
    /// </summary>
    [SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality", Justification = "Not applicable here since we want to detect changes in the value.")]
    public double Value
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
    /// Initializes a new instance of the <see cref="Slider"/> class with the provided <paramref name="min"/> value, <paramref name="max"/> value and <paramref name="value"/>.
    /// </summary>
    /// <param name="min">The maximum value for the <see cref="Slider"/> element.</param>
    /// <param name="max">The minimum value for the <see cref="Slider"/> element.</param>
    /// <param name="value">The value assigned to the <see cref="Slider"/> element.</param>
    public Slider(double min, double max, double value = 0)
    {
        if (min > max)
        {
            throw new ArgumentException($"{nameof(min)} is greater than or equal to {nameof(max)}!");
        }
        if (value > max)
        {
            throw new ArgumentException($"{nameof(value)} is greater than {nameof(max)}!");
        }
        if (value < min)
        {
            throw new ArgumentException($"{nameof(value)} is less than {nameof(min)}!");
        }

        Minimum = min;
        Maximum = max;
        Value = value;
    }

    internal override string GetJsonValue()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    internal override void SetJsonValue(string value)
    {
        // Values saved by older versions use the current culture (e.g. "30,5"), so fall back to it.
        // Thousands separators are not allowed, otherwise "30,5" would be read as 305 by the invariant culture.
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            _ = double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        Value = result;
    }
}