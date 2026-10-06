using DesktopMagic.Api.Settings;

using System.Text.Json.Serialization;

namespace DesktopMagic.Plugins;

public class SettingElement
{
    private string jsonValue = string.Empty;
    private bool shareable;

    [JsonIgnore]
    public Setting? Input { get; set; }

    public string Name { get; set; }
    public int OrderIndex { get; set; }

    public string Id { get; set; }

    /// <summary>
    /// Whether the value may be included when sharing a layout on mod.io.
    /// Free-text values (URLs, locations, file paths, ...) can contain personal data and are never shared.
    /// Persisted so the decision stays stable while the plugin is not loaded.
    /// </summary>
    public bool Shareable
    {
        get => Input is null ? shareable : IsShareableInput(Input);
        set => shareable = value;
    }

    public string JsonValue
    {
        get => Input?.GetJsonValue() ?? jsonValue;
        set
        {
            if (Input is null)
            {
                jsonValue = value;
            }
            else
            {
                Input.SetJsonValue(value);
            }
        }
    }

    public SettingElement(Setting input, string id, string name, int orderIndex)
    {
        Input = input;
        Id = id;
        Name = name;
        OrderIndex = orderIndex;
    }

    private static bool IsShareableInput(Setting input)
    {
        return input is CheckBox or ColorPicker or ComboBox or IntegerUpDown or Slider;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [JsonConstructor]
    private SettingElement()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }
}