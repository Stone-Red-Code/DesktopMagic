using DesktopMagicPluginAPI;
using System;
using System.Diagnostics;
using System.Drawing;

namespace DesktopMagicPlugin.Test
{
    public class PluginScript : Plugin
    {
        public override Bitmap Main()
        {
            Bitmap bmp = new Bitmap(100, 100);
            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Application.Color);
            return bmp;
        }

        public override void OnMouseClick(Point position)
        {
            Debug.WriteLine(position);
        }
    }
}