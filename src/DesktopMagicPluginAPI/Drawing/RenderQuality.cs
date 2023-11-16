namespace DesktopMagicPluginAPI.Drawing;

/// <summary>
/// Specifies which render quality is used to display the bitmap images.
/// </summary>
public enum RenderQuality
{
    /// <summary>
    /// Slower then <see cref="Low"/> but produces higher quality output.
    /// </summary>
    High,

    /// <summary>
    /// Faster then <see cref="High"/> but produces lower quality output.
    /// </summary>
    Low,

    /// <summary>
    /// Provides performance benefits over <see cref="Low"/>
    /// </summary>
    Performance
}