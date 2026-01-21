using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMagic.Api;

/// <summary>
/// Provides an abstract base class for creating asynchronous plugins that can be integrated into the application, supporting periodic updates,
/// rendering, and user interaction.
/// </summary>
/// <remarks>Derive from this class to implement custom plugin functionality. The class defines lifecycle methods
/// such as <see cref="Plugin.Start"/>, <see cref="Plugin.Stop"/>, and <see cref="Main"/> for activation, deactivation, and periodic
/// execution. It also provides event handlers for mouse and theme interactions, as well as access to application data
/// and rendering configuration. Implementations should override relevant methods to respond to user input, update
/// intervals, and configuration changes as needed.</remarks>
public abstract class AsyncPlugin : Plugin
{
    /// <summary>
    /// Occurs once when the plugin gets activated. Override for async initialization.
    /// </summary>
    /// <param name="cancellationToken">Token signaled when the host requests cancellation.</param>
    public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Occurs once when the plugin gets deactivated. Override for async cleanup.
    /// </summary>
    /// <param name="cancellationToken">Token signaled when the host requests cancellation.</param>
    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Occurs when the <see cref="Plugin.UpdateInterval"/> elapses.
    /// </summary>
    /// <returns></returns>
    public abstract Task<Bitmap?> MainAsync(CancellationToken cancellationToken);

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