using DesktopMagicPluginAPI.Inputs;

namespace DesktopMagic.Plugins;

internal class SettingElement
{
    public Element Element { get; }
    public string Name { get; }
    public int OrderIndex { get; }

    public SettingElement(Element element, string name, int orderIndex)
    {
        Element = element;
        Name = name;
        OrderIndex = orderIndex;
    }
}