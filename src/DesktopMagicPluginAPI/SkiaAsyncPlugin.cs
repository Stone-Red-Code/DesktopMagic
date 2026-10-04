using SkiaSharp;

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMagic.Api;

/// <summary>
/// Provides an abstract base class for creating asynchronous plugins using SkiaSharp rendering.
/// Plugins override <see cref="RenderAsync(CancellationToken)"/> and return an <see cref="SKImage"/>
/// of their own size, just like <see cref="AsyncPlugin.MainAsync(CancellationToken)"/> returns a
/// <see cref="Bitmap"/>. The returned image is scaled to fit the plugin window while preserving
/// its aspect ratio.
/// All existing <see cref="Plugin"/>, <see cref="AsyncPlugin"/>, and <see cref="SkiaPlugin"/> code is unaffected.
/// </summary>
public abstract class SkiaAsyncPlugin : AsyncPlugin
{
    /// <summary>
    /// This method is sealed and returns null for async Skia plugins.
    /// Override <see cref="RenderAsync(CancellationToken)"/> instead.
    /// </summary>
    public sealed override Task<Bitmap?> MainAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<Bitmap?>(null);
    }

    /// <summary>
    /// Called every <see cref="Plugin.UpdateInterval"/> milliseconds to render the plugin asynchronously.
    /// Return <see langword="null"/> to keep the previously rendered image.
    /// </summary>
    /// <param name="cancellationToken">Token signaled when the host requests cancellation.</param>
    /// <returns>The rendered image, or <see langword="null"/> to keep the previous frame.</returns>
    public abstract Task<SKImage?> RenderAsync(CancellationToken cancellationToken);
}
