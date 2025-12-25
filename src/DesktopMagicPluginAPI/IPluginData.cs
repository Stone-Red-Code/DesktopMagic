using System.Drawing;

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

    /// <summary>
    /// Logs a message to the application log.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The log level (Info, Warning, Error).</param>
    void Log(string message, LogLevel level = LogLevel.Info);

    /// <summary>
    /// Shows a message box to the user.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The title of the message box.</param>
    void ShowMessage(string message, string title);

    /// <summary>
    /// Saves the current state of fields and properties marked with <see cref="PersistStateAttribute"/>
    /// </summary>
    /// <remarks>
    /// State is automatically saved when the plugin stops, but this method can be called
    /// to save state at any time, such as after important user interactions.
    /// </remarks>
    void SaveState();
}