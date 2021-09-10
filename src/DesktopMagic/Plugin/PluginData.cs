using DesktopMagicPluginAPI;
using System.Drawing;

namespace DesktopMagic
{
    internal class PluginData : IPluginData
    {
        private readonly PluginWindow window;

        public PluginData(PluginWindow win)
        {
            window = win;
        }

        public string Font => MainWindow.GlobalFont;

        public Color Color => (MainWindow.GlobalSystemColor as SolidBrush).Color;

        public Point WindowSize => new Point((int)window.ActualWidth, (int)window.ActualHeight);

        public Point WindowPosition => new Point((int)window.Left, (int)window.Top);

        public void UpdateWindow() => window.UpdatePluginWindow();
    }
}