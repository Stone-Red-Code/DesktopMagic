using SkiaSharp;

using System.Drawing;

namespace DesktopMagic.Api;

/// <summary>
/// Provides an abstract base class for creating plugins using SkiaSharp rendering.
/// Plugins override <see cref="Main(SKCanvas)"/> instead of <see cref="Plugin.Main()"/>.
/// All existing <see cref="Plugin"/> and <see cref="AsyncPlugin"/> code is unaffected.
/// </summary>
public abstract class SkiaPlugin : Plugin
{
    /// <summary>
    /// This method is sealed and returns null for Skia plugins.
    /// Override <see cref="Main(SKCanvas)"/> instead.
    /// </summary>
    /// <returns>Always null.</returns>
    public sealed override Bitmap? Main()
    {
        return null;
    }

    /// <summary>
    /// Called every <see cref="Plugin.UpdateInterval"/> milliseconds to render the plugin.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    public abstract void Main(SKCanvas canvas);
}
