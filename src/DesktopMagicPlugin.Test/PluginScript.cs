using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using DesktopMagicPluginAPI.Drawing;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace DesktopMagicPlugin.Test
{
    public class InputExamplePlugin : Plugin
    {
        public override int UpdateInterval { get; set; } = 0;

        [Element("Text:")] //Mark the property as element with the specified description
        private TextBox textBox = new TextBox("abc"); //Create a text box with the specified default value.

        private Color color = Color.Black;

        public InputExamplePlugin()
        {
            textBox.OnValueChanged += TextBox_OnValueChanged; //Add an event handler to the "OnValueChanged" event.
        }

        private void TextBox_OnValueChanged()
        {
            Application.UpdateWindow(); //Update the plugin window. (Calls the "Main" method.)
        }

        public override void OnMouseMove(Point position)
        {
            Debug.WriteLine(position.X);
            Debug.WriteLine(500);
            if (500 < position.X && color == Color.Black)
            {
                color = Color.White;
                Application.UpdateWindow();
            }
            else if (500 > position.X && color == Color.White)
            {
                color = Color.Black;
                Application.UpdateWindow();
            }
        }

        public override Bitmap Main()
        {
            Bitmap bmp = new Bitmap(1000, 1000);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(color);
                g.DrawStringFixedWidth(textBox.Value, new Font(Application.Font, 100), Brushes.Black, new PointF(0, 0), 120); //Draw the value of the text box to the image.
            }

            return bmp; //Return the image.
        }
    }
}