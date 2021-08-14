using DesktopMagicPluginAPI.Inputs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagic
{
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
}