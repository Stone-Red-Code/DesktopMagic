using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Collections.Generic;

namespace DesktopMagicPlugin.Test
{
    public class GifPlugin : Plugin
    {
        [Element("Gif path:")]
        private TextBox input = new TextBox("");

        private List<Bitmap> bitmaps = new List<Bitmap>();

        public override int UpdateInterval { get; set; } = 100;
        private int frameCount = -1;

        public GifPlugin()
        {
            input.OnValueChanged += Input_OnValueChanged;
        }

        private void Input_OnValueChanged()
        {
            if (File.Exists(input.Value))
            {
                Image gif = Image.FromFile(input.Value);
                bitmaps.Clear();
                for (int i = 0; i < gif.GetFrameCount(FrameDimension.Time); i++)
                {
                    gif.SelectActiveFrame(FrameDimension.Time, i);

                    bitmaps.Add(new Bitmap(gif));
                }
            }
        }

        public override Bitmap Main()
        {
            if (bitmaps.Count == 0)
                return new Bitmap(1, 1);

            frameCount++;

            if (frameCount >= bitmaps.Count)
                frameCount = 0;

            return bitmaps[frameCount];
        }
    }
}