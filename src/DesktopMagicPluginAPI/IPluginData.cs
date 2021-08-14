using System.Drawing;

namespace DesktopMagicPluginAPI
{
    public interface IPluginData
    {
        string Font { get; }
        Color Color { get; }
        Point WindowSize { get; }
        Point WindowPosition { get; }

        void UpdateWindow();
    }
}