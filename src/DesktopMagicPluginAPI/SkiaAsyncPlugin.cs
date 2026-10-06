using SkiaSharp;

using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMagic.Api;

/// <summary>
/// Infrastructure base class of <see cref="SkiaAsyncPlugin"/>. Derive from <see cref="SkiaAsyncPlugin"/> instead.
/// </summary>
/// <remarks>This class only exists to seal the <see cref="Bitmap"/> based <see cref="AsyncPlugin.MainAsync"/> method,
/// so that <see cref="SkiaAsyncPlugin"/> can declare its own <see cref="SkiaAsyncPlugin.MainAsync"/> method returning an <see cref="SKImage"/>.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SkiaAsyncPluginBase : AsyncPlugin
{
    /// <summary>
    /// This method should not be called directly! Override and use <see cref="SkiaAsyncPlugin.MainAsync"/> for asynchronous Skia plugin rendering.
    /// </summary>
    /// <remarks>This method only exists to fulfill base class requirements.</remarks>
    /// <param name="cancellationToken">Token signaled when the host requests cancellation.</param>
    /// <returns>A task that always resolves to <see langword="null"/>.</returns>
    public sealed override Task<Bitmap?> MainAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<Bitmap?>(null);
    }
}

/// <summary>
/// Provides an abstract base class for creating asynchronous plugins that render with SkiaSharp and can be integrated into the application,
/// supporting periodic updates, rendering, and user interaction.
/// </summary>
/// <remarks>Derive from this class to implement custom plugin functionality. The class defines lifecycle methods
/// such as <see cref="AsyncPlugin.StartAsync"/>, <see cref="AsyncPlugin.StopAsync"/>, and <see cref="MainAsync"/> for activation, deactivation, and periodic
/// execution. It also provides event handlers for mouse and theme interactions, as well as access to application data
/// and rendering configuration. Implementations should override relevant methods to respond to user input, update
/// intervals, and configuration changes as needed. The image returned by <see cref="MainAsync"/> is scaled to fit the
/// plugin window while preserving its aspect ratio.</remarks>
public abstract class SkiaAsyncPlugin : SkiaAsyncPluginBase
{
    /// <summary>
    /// Occurs when the <see cref="Plugin.UpdateInterval"/> elapses.
    /// </summary>
    /// <param name="cancellationToken">Token signaled when the host requests cancellation.</param>
    /// <returns>The rendered image, or <see langword="null"/> to keep the previous frame.</returns>
    public new abstract Task<SKImage?> MainAsync(CancellationToken cancellationToken);
}
