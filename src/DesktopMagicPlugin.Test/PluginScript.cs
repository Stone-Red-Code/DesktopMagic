using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using System.Drawing;
using System.Diagnostics;

namespace DesktopMagicPlugin.Test
{
    public class GifPlugin : Plugin
    {
        [Element("Gif path:")]
        public TextBox input = new TextBox("");

        public override int UpdateInterval { get; set; } = 100;

        public override void OnMouseClick(Point position, MouseButton mouseButton)
        {
            Debug.WriteLine(position + " | " + mouseButton);
        }

        public override Bitmap Main()
        {
            Bitmap bmp = new Bitmap(100, 100);
            using Graphics graphics = Graphics.FromImage(bmp);
            graphics.Clear(Color.Red);
            return bmp;
        }
    }
}