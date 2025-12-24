using System;
using System.Drawing;

namespace DesktopMagic.Api;

/// <summary>
///  Defines properties and methods that provide information about the main application.
/// </summary>
public interface IPluginData
{
    /// <summary>
    /// Occurs when the application's theme has changed.
    /// </summary>
    /// <remarks>Subscribe to this event to be notified when the theme changes, allowing UI elements or
    /// components to update their appearance accordingly. The event is raised after the theme change has been
    /// applied.</remarks>
    event EventHandler? ThemeChanged;

    /// <summary>
    /// Gets the current theme setting of the main application.
    /// </summary>
    ITheme Theme { get; }

    /// <summary>
    /// Gets the window size of the plugin window.
    /// </summary>
    Size WindowSize { get; }

    /// <summary>
    /// Gets the window position of the plugin window.
    /// </summary>
    Point WindowPosition { get; }

    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    string PluginName { get; }

    /// <summary>
    /// Gets the path of the parent directory of the plugin.
    /// </summary>
    string PluginPath { get; }

    /// <summary>
    /// Updates the plugin window.
    /// </summary>
    void UpdateWindow();
}