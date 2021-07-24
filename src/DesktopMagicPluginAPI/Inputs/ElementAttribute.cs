using System;

namespace DesktopMagicPluginAPI.Inputs
{
    [AttributeUsage(AttributeTargets.Field)
]
    public class ElementAttribute : Attribute
    {
        public string Name { get; }
        public int OrderIndex { get; }

        public ElementAttribute(string name = "", int orderIndex = 0)
        {
            Name = name;
            OrderIndex = orderIndex;
        }

        public ElementAttribute(int orderIndex = 0)
        {
            OrderIndex = orderIndex;
        }

        public ElementAttribute()
        {
        }
    }
}