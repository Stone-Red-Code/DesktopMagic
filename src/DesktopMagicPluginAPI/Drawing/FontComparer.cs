using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Drawing
{
    internal class FontComparer : IEqualityComparer<Font>
    {
        public bool Equals(Font font1, Font font2)
        {
            if (font1.Name != font2.Name) return false;
            if (font1.SizeInPoints != font2.SizeInPoints) return false;
            if (font1.Style != font2.Style) return false;
            return true;
        }

        public int GetHashCode([DisallowNull] Font obj)
        {
            return obj.GetHashCode();
        }
    }
}