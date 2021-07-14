using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    public class Heading : Element
    {
        public string Value { get; }

        public Heading(string value)
        {
            Value = value;
        }
    }
}