using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using DesktopMagicPluginAPI.Drawing;
using System.Drawing;

namespace DesktopMagicPlugin.Test
{
    public class InputExamplePlugin : Plugin
    {
        public override int UpdateInterval { get; set; } = 0;

        [Element("Text:")] //Mark the property as element with the specified description
        private TextBox textBox = new TextBox("abc"); //Create a text box with the specified default value.

        public InputExamplePlugin()
        {
            textBox.OnValueChanged += TextBox_OnValueChanged; //Add an event handler to the "OnValueChanged" event.
        }

        private void TextBox_OnValueChanged()
        {
            Application.UpdateWindow(); //Update the pugin window. (Calls the "Main" method.)
        }

        public override Bitmap Main()
        {
            Bitmap bmp = new Bitmap(1000, 1000);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Application.Color); //Set the background color to the color specified in the Desktop Magic application.

                g.DrawStringFixedWidth(textBox.Value, new Font(Application.Font, 100), Brushes.Black, new PointF(0, 0), 120); //Draw the value of the text box to the image.
            }

            return bmp; //Return the image.
        }
    }
}