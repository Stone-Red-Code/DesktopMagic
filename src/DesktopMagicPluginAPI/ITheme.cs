using System.Drawing;

namespace DesktopMagicPluginAPI
{
    /// <summary>
    /// The theme settings of the main application.
    /// </summary>
    public interface ITheme
    {
        /// <summary>
        /// Gets the primary color of the main application.
        /// </summary>
        Color PrimaryColor { get; }

        /// <summary>
        /// Gets the secondary color of the main application.
        /// </summary>
        Color SecondaryColor { get; }

        /// <summary>
        /// Gets the background color of the main application.
        /// </summary>
        Color BackgroundColor { get; }

        /// <summary>
        /// Gets the font of the main application.
        /// </summary>
        string Font { get; }
    }
}