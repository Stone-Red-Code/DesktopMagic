using SkiaSharp;

using System.Drawing;

namespace DesktopMagic.Api;

/// <summary>
/// Provides an abstract base class for creating plugins using SkiaSharp rendering.
/// Plugins override <see cref="Render()"/> and return an <see cref="SKImage"/> of their own size,
/// just like <see cref="Plugin.Main()"/> returns a <see cref="Bitmap"/>. The returned image is
/// scaled to fit the plugin window while preserving its aspect ratio.
/// All existing <see cref="Plugin"/> and <see cref="AsyncPlugin"/> code is unaffected.
/// </summary>
public abstract class SkiaPlugin : Plugin
{
    /// <summary>
    /// This method is sealed and returns null for Skia plugins.
    /// Override <see cref="Render()"/> instead.
    /// </summary>
    /// <returns>Always null.</returns>
    public sealed override Bitmap? Main()
    {
        return null;
    }

    /// <summary>
    /// Called every <see cref="Plugin.UpdateInterval"/> milliseconds to render the plugin.
    /// Return <see langword="null"/> to keep the previously rendered image.
    /// </summary>
    /// <returns>The rendered image, or <see langword="null"/> to keep the previous frame.</returns>
    public abstract SKImage? Render();
}
