﻿using System.Collections.ObjectModel;

namespace DesktopMagic.Api.Settings;

/// <summary>
/// Represents a selection control with a drop-down list.
/// </summary>
public class ComboBox : Setting
{
    private string _value = string.Empty;

    /// <summary>
    /// Gets the collection used to generate the content of the <see cref="ComboBox"/>.
    /// </summary>
    public ObservableCollection<string> Items { get; } = [];

    /// <summary>
    /// Gets the currently selected item associated with this <see cref="ComboBox"/>.
    /// </summary>
    /// <remarks>If you assign a value to this property, the displayed text in the user interface will not be changed.</remarks>
    public string Value
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
    /// Initializes a new instance of the <see cref="Label"/> class with the provided <paramref name="items"/>.
    /// </summary>
    /// <param name="items"></param>
    public ComboBox(params string[] items)
    {
        Value = items[0];

        foreach (string item in items)
        {
            Items.Add(item);
        }
    }

    internal override string GetJsonValue()
    {
        return Value;
    }

    internal override void SetJsonValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        Value = value;
    }
}