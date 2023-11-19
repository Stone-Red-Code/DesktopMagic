using DesktopMagicPluginAPI.Settings;

namespace DesktopMagic.Plugins;

public class InputElement(Setting element, string name, int orderIndex)
{
    public Setting Input { get; } = element;
    public string Name { get; } = name;
    public int OrderIndex { get; } = orderIndex;
}