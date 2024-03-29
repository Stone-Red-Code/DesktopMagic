﻿using System.Drawing;

namespace DesktopMagic.Api;

/// <summary>
///  Defines properties and methods that provide information about the main application.
/// </summary>
public interface IPluginData
{
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