using System;
using System.Drawing;
using System.Threading.Tasks;

namespace DesktopMagic.Api;

/// <summary>
/// Provides an abstract base class for plugins that execute asynchronously. Derive from this class to implement plugins
/// whose main logic runs in a non-blocking manner using asynchronous operations.
/// </summary>
/// <remarks>AsyncPlugin is intended for scenarios where plugin execution should not block the calling thread.
/// Override the MainAsync method to implement asynchronous plugin behavior. The synchronous Main method is sealed and
/// obsolete; it cannot be used for execution and will always throw an exception. Use MainAsync for all plugin logic.
/// The MainAsync method is typically invoked when the UpdateInterval elapses, allowing periodic asynchronous
/// execution.</remarks>
public abstract class AsyncPlugin : Plugin
{
    /// <summary>
    /// Occurs when the <see cref="Plugin.UpdateInterval"/> elapses.
    /// </summary>
    /// <returns></returns>
    public abstract Task<Bitmap?> MainAsync();

    /// <summary>
    /// This method should not be called directly! Override and use <see cref="MainAsync"/> for asynchronous plugin operations.
    /// </summary>
    /// <remarks>This method only exists to fulfill base class requirements.</remarks>
    /// <returns>Calling it will always result in an <see cref="InvalidOperationException"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this method is called directly. Use MainAsync for plugin execution instead.</exception>
    public sealed override Bitmap? Main()
    {
        throw new InvalidOperationException($"AsyncPlugin.Main() should not be called directly. Override {nameof(MainAsync)} instead.");
    }
}