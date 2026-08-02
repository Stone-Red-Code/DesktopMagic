using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace DesktopMagic.Plugins;

/// <summary>
/// Keeps the per-instance plugin settings of every window sharing the same layout in sync.
/// Each window owns its own <see cref="DesktopMagic.Api.Settings.Setting"/> objects (plugins
/// hold them in readonly fields and subscribe to them in Start), so a value change on one
/// window has to be mirrored onto the sibling windows.
/// </summary>
internal sealed class SettingSynchronizer
{
    private readonly object _lock = new();
    private readonly List<IPluginWindow> windows = [];
    private readonly HashSet<(IPluginWindow Window, string Id)> inFlight = [];

    /// <summary>
    /// Registers a window so it receives mirrored settings from the other windows of this layout.
    /// </summary>
    public void Register(IPluginWindow window)
    {
        lock (_lock)
        {
            if (!windows.Contains(window))
            {
                windows.Add(window);
            }
        }
    }

    /// <summary>
    /// Unregisters a window and returns whether no windows remain.
    /// </summary>
    public bool Unregister(IPluginWindow window)
    {
        lock (_lock)
        {
            windows.Remove(window);
            return windows.Count == 0;
        }
    }

    /// <summary>
    /// Mirrors a setting value change onto every other registered window.
    /// </summary>
    public void SettingChanged(IPluginWindow origin, string id, string value)
    {
        Mirror(origin, id, window => window.ApplySettingValue(id, value));
    }

    /// <summary>
    /// Mirrors a button click onto every other registered window.
    /// </summary>
    public void ButtonClicked(IPluginWindow origin, string id)
    {
        Mirror(origin, id, window => window.ApplyButtonClick(id));
    }

    private void Mirror(IPluginWindow origin, string id, Action<IPluginWindow> apply)
    {
        IPluginWindow[] snapshot;
        lock (_lock)
        {
            snapshot = windows.ToArray();
        }

        foreach (IPluginWindow window in snapshot)
        {
            if (ReferenceEquals(window, origin) || !window.IsRunning)
            {
                continue;
            }

            lock (_lock)
            {
                if (!inFlight.Add((window, id)))
                {
                    continue;
                }
            }

            if (window is not DispatcherObject dispatcherObject)
            {
                lock (_lock)
                {
                    inFlight.Remove((window, id));
                }
                continue;
            }

            _ = dispatcherObject.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    apply(window);
                }
                finally
                {
                    lock (_lock)
                    {
                        inFlight.Remove((window, id));
                    }
                }
            });
        }
    }
}
