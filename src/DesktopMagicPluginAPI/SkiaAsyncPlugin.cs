using SkiaSharp;

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMagic.Api;

/// <summary>
/// Provides an abstract base class for creating asynchronous plugins using SkiaSharp rendering.
/// Plugins override <see cref="MainAsync(SKCanvas, CancellationToken)"/> instead of <see cref="Plugin.Main()"/>.
/// All existing <see cref="Plugin"/>, <see cref="AsyncPlugin"/>, and <see cref="SkiaPlugin"/> code is unaffected.
/// </summary>
public abstract class SkiaAsyncPlugin : AsyncPlugin
{
    /// <summary>
    /// This method is sealed and returns null for async Skia plugins.
    /// Override <see cref="MainAsync(SKCanvas, CancellationToken)"/> instead.
    /// </summary>
    public sealed override Task<Bitmap?> MainAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<Bitmap?>(null);
    }

    /// <summary>
    /// Called every <see cref="Plugin.UpdateInterval"/> milliseconds to render the plugin asynchronously.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    /// <param name="cancellationToken">Token signaled when the host requests cancellation.</param>
    public abstract Task MainAsync(SKCanvas canvas, CancellationToken cancellationToken);
}
