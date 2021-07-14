using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    [AttributeUsage(AttributeTargets.Field)
]
    public class ElementAttribute : Attribute
    {
        public string Name { get; }
        public int OrderIndex { get; }

        public ElementAttribute(string name, int orderIndex = 0)
        {
            Name = name;
            OrderIndex = orderIndex;
        }
    }
}