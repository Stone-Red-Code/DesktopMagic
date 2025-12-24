using DesktopMagic.Api;
using DesktopMagic.Settings;

using System;
using System.Drawing;

namespace DesktopMagic.Plugins;

internal class PluginData(PluginWindow window, PluginSettings pluginSettings) : IPluginData
{
    private readonly PluginWindow window = window;

    public event EventHandler? ThemeChanged;

    public ITheme Theme => pluginSettings.Theme;

    public Size WindowSize => new Size((int)window.ActualWidth, (int)window.ActualHeight);

    public Point WindowPosition => new Point((int)window.Left, (int)window.Top);

    public string PluginName => window.PluginMetadata.Name;

    public string PluginPath => window.PluginFolderPath;

    public void UpdateWindow()
    {
        window.UpdatePluginWindow();
    }

    internal void NotifyThemeChanged()
    {
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}