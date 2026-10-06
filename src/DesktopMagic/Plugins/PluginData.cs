using CuteUtils.Logging;

using DesktopMagic.Api;
using DesktopMagic.Settings;

using System;
using System.Drawing;

using Wpf.Ui.Controls;

namespace DesktopMagic.Plugins;

internal class PluginData(PluginWindow window, PluginSettings pluginSettings) : IPluginData
{
    private readonly PluginWindow window = window;
    private Size windowPixelSize = new(1, 1);

    public ITheme Theme => pluginSettings.Theme;

    public Size WindowSize => new Size((int)window.ActualWidth, (int)window.ActualHeight);

    public Size WindowPixelSize => windowPixelSize;

    /// <summary>
    /// Refreshes the cached device-pixel window size. Must be called from the UI thread
    /// so the value can be read safely by plugins on their render thread.
    /// </summary>
    internal void UpdateWindowPixelSize(int width, int height)
    {
        windowPixelSize = new Size(Math.Max(1, width), Math.Max(1, height));
    }

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

        App.Logger.Log($"\"{PluginName}\" - {message}", "Plugin", severity);
    }

    public void ShowMessage(string message, string? title = null)
    {
        title ??= PluginName;

        _ = window.Dispatcher.Invoke(async () =>
        {
            System.Windows.Window temporaryOwner = new()
            {
                AllowsTransparency = true,
                ShowInTaskbar = false,
                WindowStyle = System.Windows.WindowStyle.None,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
            };

            temporaryOwner.Show();

            MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = temporaryOwner,
                Title = $"{App.AppName} - {title}",
                Content = message,
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();

            temporaryOwner.Close();
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