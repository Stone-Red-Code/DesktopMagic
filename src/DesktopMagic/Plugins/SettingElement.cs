using DesktopMagicPluginAPI.Inputs;

namespace DesktopMagic.Plugins;

internal class SettingElement(Element element, string name, int orderIndex)
{
    public Element Element { get; } = element;
    public string Name { get; } = name;
    public int OrderIndex { get; } = orderIndex;
}