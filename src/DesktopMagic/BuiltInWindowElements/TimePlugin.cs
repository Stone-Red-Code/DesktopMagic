using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Drawing;

using System;
using System.Drawing;
using System.Drawing.Text;

namespace DesktopMagic.BuiltInWindowElements
{
    internal class TimePlugin : Plugin
    {
        public override int UpdateInterval => 1000;

        public override Bitmap Main()
        {
            string time = DateTime.Now.ToLongTimeString();

            Font font = new Font(Application.Theme.Font, 200);

            Bitmap bmp = new Bitmap(1, 1);
            using Graphics tmpGr = Graphics.FromImage(bmp);
            tmpGr.TextRenderingHint = TextRenderingHint.AntiAlias;

            SizeF size = CalculateSize(tmpGr, font);

            bmp = new Bitmap((int)size.Width, (int)size.Height);
            bmp.SetResolution(100, 100);

            using Graphics gr = Graphics.FromImage(bmp);

            gr.TextRenderingHint = TextRenderingHint.AntiAlias;
            gr.DrawStringNoLeftPadding(time, font, new SolidBrush(Application.Theme.PrimaryColor), 0, 0);

            return bmp;
        }

        private SizeF CalculateSize(Graphics graphics, Font font)
        {
            string template = "##:##:##";
            SizeF size = new SizeF();
            for (int i = 0; i < 9; i++)
            {
                SizeF newSize = graphics.MeasureStringNoLeftPadding(template.Replace("#", i.ToString()), font);

                size.Width = Math.Max(size.Width, newSize.Width);
                size.Height = Math.Max(size.Height, newSize.Height);
            }
            return size;
        }
    }
}