using CuteUtils.Logging;

using DesktopMagic.Api;
using DesktopMagic.Settings;

using System.Drawing;

namespace DesktopMagic.Plugins;

internal class PluginData(PluginWindow window, PluginSettings pluginSettings) : IPluginData
{
    private readonly PluginWindow window = window;

    public ITheme Theme => pluginSettings.Theme;

    public Size WindowSize => new Size((int)window.ActualWidth, (int)window.ActualHeight);

    public Point WindowPosition => new Point((int)window.Left, (int)window.Top);

    public string PluginName => window.PluginMetadata.Name;

    public string PluginPath => window.PluginFolderPath;

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        LogSeverity severity = level switch
        {
            LogLevel.Info => LogSeverity.Info,
            LogLevel.Warning => LogSeverity.Warn,
            LogLevel.Error => LogSeverity.Error,
            _ => LogSeverity.Info,
        };

        App.Logger.Log($"\"{PluginName}\" - Stopping plugin", "Plugin", severity);
    }

    public void ShowMessage(string message, string title)
    {
        window.Dispatcher.Invoke(() =>
        {
            _ = System.Windows.MessageBox.Show(window, message, title);
        });
    }

    public void UpdateWindow()
    {
        window.UpdatePluginWindow();
    }

    public void SaveState()
    {
        window.SavePluginState();
    }
}