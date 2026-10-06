using SkiaSharp;

using System.ComponentModel;
using System.Drawing;

namespace DesktopMagic.Api;

/// <summary>
/// Infrastructure base class of <see cref="SkiaPlugin"/>. Derive from <see cref="SkiaPlugin"/> instead.
/// </summary>
/// <remarks>This class only exists to seal the <see cref="Bitmap"/> based <see cref="Plugin.Main"/> method,
/// so that <see cref="SkiaPlugin"/> can declare its own <see cref="SkiaPlugin.Main"/> method returning an <see cref="SKImage"/>.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SkiaPluginBase : Plugin
{
    /// <summary>
    /// This method should not be called directly! Override and use <see cref="SkiaPlugin.Main"/> for Skia plugin rendering.
    /// </summary>
    /// <remarks>This method only exists to fulfill base class requirements.</remarks>
    /// <returns>Always <see langword="null"/>.</returns>
    public sealed override Bitmap? Main()
    {
        return null;
    }
}

/// <summary>
/// Provides an abstract base class for creating plugins that render with SkiaSharp and can be integrated into the application,
/// supporting periodic updates, rendering, and user interaction.
/// </summary>
/// <remarks>Derive from this class to implement custom plugin functionality. The class defines lifecycle methods
/// such as <see cref="Plugin.Start"/>, <see cref="Plugin.Stop"/>, and <see cref="Main"/> for activation, deactivation, and periodic
/// execution. It also provides event handlers for mouse and theme interactions, as well as access to application data
/// and rendering configuration. Implementations should override relevant methods to respond to user input, update
/// intervals, and configuration changes as needed. The image returned by <see cref="Main"/> is scaled to fit the
/// plugin window while preserving its aspect ratio.</remarks>
public abstract class SkiaPlugin : SkiaPluginBase
{
    /// <summary>
    /// Occurs when the <see cref="Plugin.UpdateInterval"/> elapses.
    /// </summary>
    /// <returns>The rendered image, or <see langword="null"/> to keep the previous frame.</returns>
    public new abstract SKImage? Main();
}
