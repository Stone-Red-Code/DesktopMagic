using System.Collections.ObjectModel;
using System.Text.Json;

namespace DesktopMagic.Api.Settings;

/// <summary>
/// Represents a selection control with a drop-down list.
/// </summary>
public class ComboBox : Setting
{
    private string _value;

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
        foreach (string item in items)
        {
            Items.Add(item);
        }
    }

    internal override string GetJsonValue()
    {
        return JsonSerializer.Serialize(new { Value, Items });
    }

    internal override void SetJsonValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        JsonElement json = JsonSerializer.Deserialize<JsonElement>(value);
        Items.Clear();
        foreach (JsonElement item in json.GetProperty(nameof(Items)).EnumerateArray())
        {
            Items.Add(item.GetString() ?? string.Empty);
        }
        Value = json.GetProperty(nameof(Value)).GetString() ?? string.Empty;
    }
}