using System.Drawing;

namespace DesktopMagicPluginAPI
{
    /// <summary>
    ///  Defines properties and methods that provide information about the main application.
    /// </summary>
    public interface IPluginData
    {
        /// <summary>
        /// Gets the font of the main application.
        /// </summary>
        string Font { get; }

        /// <summary>
        /// Gets the color of the main application.
        /// </summary>
        Color Color { get; }

        /// <summary>
        /// Gets the window size of the plugin window.
        /// </summary>
        Point WindowSize { get; }

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
}