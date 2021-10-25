using DesktopMagicPluginAPI;

using System.Drawing;

namespace DesktopMagic.Plugins
{
    internal class Theme : ITheme
    {
        public Color PrimaryColor { get; set; } = Color.White;

        public Color SecondaryColor { get; set; } = Color.White;

        public Color BackgroundColor { get; set; } = Color.Transparent;

        public string Font { get; internal set; } = "Segoe UI";

        public System.Windows.Media.Brush PrimaryBrush { get; set; } = System.Windows.Media.Brushes.White;
        public System.Windows.Media.Brush SecondaryBrush { get; set; } = System.Windows.Media.Brushes.White;
        public System.Windows.Media.Brush BackgroundBrush { get; set; } = System.Windows.Media.Brushes.Transparent;
    }
}