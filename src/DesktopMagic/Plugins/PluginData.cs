using DesktopMagic.Settings;

using DesktopMagicPluginAPI;

using System.Drawing;

namespace DesktopMagic.Plugins;

internal class PluginData(PluginWindow window, PluginSettings pluginSettings) : IPluginData
{
    private readonly PluginWindow window = window;

    public ITheme Theme { get; } = pluginSettings.Theme;

    public Size WindowSize => new Size((int)window.ActualWidth, (int)window.ActualHeight);

    public Point WindowPosition => new Point((int)window.Left, (int)window.Top);

    public string PluginName => window.PluginName;

    public string PluginPath => window.PluginFolderPath;

    public void UpdateWindow()
    {
        window.UpdatePluginWindow();
    }
}