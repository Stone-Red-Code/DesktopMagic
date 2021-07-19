using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using System;
using System.Diagnostics;
using System.Drawing;

namespace DesktopMagicPlugin.Test
{
    public class PluginScript : Plugin
    {
        [Element]
        private Label heading = new Label("Heading", true);

        [Element]
        private Label label = new Label("");

        [Element("Slider:")]
        private Slider slider = new Slider(1, 23, 4);

        [Element("Text:")]
        private TextBox textBox = new TextBox("abc");

        [Element]
        private Button button = new Button("Press me!");

        public override int UpdateInterval { get; set; }

        public PluginScript()
        {
            slider.OnValueChanged += Slider_OnValueChanged;
            textBox.OnValueChanged += TextBox_OnValueChanged;
            button.OnClick += Button_OnClick; ;
        }

        private void Button_OnClick()
        {
            Debug.WriteLine("Button pressed");
            Application.UpdateWindow();
        }

        private void TextBox_OnValueChanged()
        {
            Debug.WriteLine("TextBox value changed:" + textBox.Value);
        }

        private void Slider_OnValueChanged()
        {
            Debug.WriteLine("Slider value changed:" + slider.Value);
        }

        public override Bitmap Main()
        {
            string str = $"{textBox.Value}: {slider.Value}";

            Bitmap bmp = new Bitmap(100, 100);
            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Application.Color);
            g.DrawString(str, new Font("arial", 10), Brushes.Black, new PointF(0, 0));

            label.Value = str;
            slider.Value++;

            return bmp;
        }
    }
}