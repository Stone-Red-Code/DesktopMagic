﻿using System;

namespace DesktopMagic.Api.Settings;

/// <summary>
/// Represents a up-down control.
/// </summary>
public class IntegerUpDown : Setting
{
    private int _value;

    /// <summary>
    /// Gets or sets the maximum value for the <see cref="IntegerUpDown"/> element.
    /// </summary>
    public int Maximum { get; }

    /// <summary>
    /// Gets or sets the minimum value for the <see cref="IntegerUpDown"/> element.
    /// </summary>
    public int Minimum { get; }

    /// <summary>
    /// Gets or sets the value assigned to the <see cref="IntegerUpDown"/> element.
    /// </summary>
    public int Value
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
    /// Initializes a new instance of the <see cref="IntegerUpDown"/> class with the provided <paramref name="min"/> value, <paramref name="max"/> value and <paramref name="value"/>.
    /// </summary>
    /// <param name="min">The maximum value for the <see cref="IntegerUpDown"/> element.</param>
    /// <param name="max">The minimum value for the <see cref="IntegerUpDown"/> element.</param>
    /// <param name="value">The value assigned to the <see cref="IntegerUpDown"/> element.</param>
    /// <exception cref="ArgumentException"></exception>
    public IntegerUpDown(int min, int max, int value = 0)
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
        return Value.ToString();
    }

    internal override void SetJsonValue(string value)
    {
        _ = int.TryParse(value, out int result);
        Value = result;
    }
}