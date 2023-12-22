using DesktopMagic.Api.Settings;

using System.Text.Json.Serialization;

namespace DesktopMagic.Plugins;

public class SettingElement
{
    private string jsonValue = string.Empty;

    [JsonIgnore]
    public Setting Input { get; set; }

    public string Name { get; set; }
    public int OrderIndex { get; set; }

    public string Id { get; set; }

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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [JsonConstructor]
    private SettingElement()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }
}