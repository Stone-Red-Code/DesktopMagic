using DesktopMagicPluginAPI.Settings;

using System.Text.Json.Serialization;

namespace DesktopMagic.Plugins;

public class InputElement
{
    public Setting Input { get; }
    public string Name { get; }
    public int OrderIndex { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public InputElement(Setting input, string name, int orderIndex)
    {
        Input = input;
        Name = name;
        OrderIndex = orderIndex;
    }

    [JsonConstructor]
    private InputElement()
    {
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}