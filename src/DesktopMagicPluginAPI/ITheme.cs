using System.Drawing;

namespace DesktopMagicPluginAPI
{
    /// <summary>
    /// The theme settings of the main application.
    /// </summary>
    public interface ITheme
    {
        /// <summary>
        /// Gets the primary color of the current theme.
        /// </summary>
        Color PrimaryColor { get; }

        /// <summary>
        /// Gets the secondary color of the current theme.
        /// </summary>
        Color SecondaryColor { get; }

        /// <summary>
        /// Gets the background color of the current theme.
        /// </summary>
        Color BackgroundColor { get; }

        /// <summary>
        /// Gets the font of the current theme.
        /// </summary>
        string Font { get; }

        /// <summary>
        /// Gets the corner radius of the current theme.
        /// </summary>
        int CornerRadius { get; }

        /// <summary>
        /// Gets the corner radius of the current theme.
        /// </summary>
        int Margin { get; }
    }
}