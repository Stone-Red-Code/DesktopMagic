using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DesktopMagic.Helpers;

/// <summary>
/// Periodically checks whether plugin windows are occluded by other top-level windows and pauses
/// their rendering while they are (a coverage-based pause algorithm, same idea as Lively's).
/// Pausing stops the plugin update timer, which stops re-rendering and re-uploading frames to the
/// GPU, so fully covered widgets consume almost no resources.
/// </summary>
internal sealed class OcclusionMonitor : IDisposable
{
    private const int CheckIntervalMs = 100;

    // Pause only when essentially the whole window is covered so widgets stay responsive even
    // when a corner is overlapped by another window.
    private const double CoverageThreshold = 0.95;

    private readonly System.Timers.Timer timer = new(CheckIntervalMs);
    private readonly Dictionary<IPluginWindow, bool> lastPaused = [];
    private readonly object gate = new();
    private bool disposed;

    // (window, handle) pairs of the plugin windows, refreshed asynchronously on the UI thread.
    private volatile IReadOnlyList<(IPluginWindow Window, nint Hwnd)> windowSnapshot = [];
    private int snapshotRefreshPending;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        timer.Elapsed += Timer_Elapsed;
        timer.Start();
        IsRunning = true;
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        // System.Timers.Timer can re-enter this handler on another ThreadPool thread when the
        // previous invocation is still running, so serialize the whole pass.
        if (!Monitor.TryEnter(gate))
        {
            return;
        }

        try
        {
            RunOcclusionPass();
        }
        finally
        {
            Monitor.Exit(gate);
        }
    }

    private void RunOcclusionPass()
    {
        RequestSnapshotRefresh();

        IReadOnlyList<(IPluginWindow Window, nint Hwnd)> windows = windowSnapshot;
        HashSet<IPluginWindow> live = [.. windows.Select(item => item.Window)];

        Dictionary<IPluginWindow, bool> pausedStates;
        try
        {
            pausedStates = ComputePausedStates(windows);
        }
        catch
        {
            return;
        }

        foreach ((IPluginWindow window, bool paused) in pausedStates)
        {
            if (lastPaused.TryGetValue(window, out bool previous) && previous == paused)
            {
                continue;
            }

            lastPaused[window] = paused;

            try
            {
                _ = ((Window)window).Dispatcher.InvokeAsync(() => window.SetRenderPaused(paused));
            }
            catch
            {
                // Window is closing; ignore.
            }
        }

        // Drop tracking for windows that no longer exist.
        foreach (IPluginWindow stale in lastPaused.Keys.Where(key => !live.Contains(key)).ToArray())
        {
            _ = lastPaused.Remove(stale);
        }
    }

    /// <summary>
    /// Posts a refresh of <see cref="windowSnapshot"/> to the UI thread without waiting for it.
    /// The manager mutates PluginWindows on the UI thread and window handles can only be obtained
    /// there; a snapshot that is one pass old is good enough, and the timer thread never blocks.
    /// </summary>
    private void RequestSnapshotRefresh()
    {
        // Skip when the previous refresh has not run yet (e.g. the UI thread is busy).
        if (Interlocked.Exchange(ref snapshotRefreshPending, 1) != 0)
        {
            return;
        }

        try
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    windowSnapshot = Manager.Instance.PluginWindows
                        .Where(window => window.IsRunning)
                        .Select(window => (Window: window, Hwnd: new WindowInteropHelper((Window)window).Handle))
                        .Where(item => item.Hwnd != nint.Zero)
                        .ToList();
                }
                finally
                {
                    _ = Interlocked.Exchange(ref snapshotRefreshPending, 0);
                }
            }, DispatcherPriority.Background);
        }
        catch
        {
            // The application is shutting down.
            _ = Interlocked.Exchange(ref snapshotRefreshPending, 0);
        }
    }

    /// <summary>
    /// Decides for every plugin window whether it is occluded, using a single top-to-bottom pass
    /// over all top-level windows. EnumWindows enumerates in z-order (front to back), so every
    /// window seen before a plugin window is above it. The pass stops at the lowest plugin window.
    /// </summary>
    private static Dictionary<IPluginWindow, bool> ComputePausedStates(IReadOnlyList<(IPluginWindow Window, nint Hwnd)> windows)
    {
        Dictionary<IPluginWindow, bool> pausedStates = [];

        // Plugin windows that have not been reached in z-order yet.
        Dictionary<nint, (IPluginWindow Window, Rectangle Area)> remaining = [];
        foreach ((IPluginWindow window, nint hwnd) in windows)
        {
            if (W32.GetWindowRect(hwnd, out W32.RECT rect))
            {
                remaining[hwnd] = (window, rect);
            }
        }

        if (remaining.Count == 0)
        {
            return pausedStates;
        }

        uint currentProcessId = (uint)Environment.ProcessId;

        // Visible windows above the current z-order position that overlap a remaining plugin window.
        List<Rectangle> occluders = [];

        _ = W32.EnumWindows((hwnd, _) =>
        {
            if (remaining.Remove(hwnd, out (IPluginWindow Window, Rectangle Area) target))
            {
                double coverage = ComputeCoverage(target.Area, occluders);
                pausedStates[target.Window] = IsForegroundCovering(target.Area, hwnd) || coverage >= CoverageThreshold;

                // Windows below the lowest plugin window cannot occlude anything.
                return remaining.Count > 0;
            }

            if (TryGetOccluderRect(hwnd, currentProcessId, remaining.Values, out Rectangle occluderRect))
            {
                occluders.Add(occluderRect);
            }

            return true;
        }, nint.Zero);

        return pausedStates;
    }

    /// <summary>
    /// Returns the bounds of <paramref name="hwnd"/> when it is a visible window from another process
    /// that overlaps at least one of <paramref name="targets"/>. Cheap checks run first so the more
    /// expensive ones only run for windows that actually overlap a plugin window.
    /// </summary>
    private static bool TryGetOccluderRect(nint hwnd, uint currentProcessId, IEnumerable<(IPluginWindow Window, Rectangle Area)> targets, out Rectangle occluderRect)
    {
        occluderRect = Rectangle.Empty;

        // Only windows from other processes occlude our widgets.
        if (!W32.IsWindowVisible(hwnd)
            || W32.GetWindowThreadProcessId(hwnd, out uint processId) == 0
            || processId == currentProcessId
            || !W32.GetWindowRect(hwnd, out W32.RECT rect))
        {
            return false;
        }

        Rectangle bounds = rect;
        if (!targets.Any(target => target.Area.IntersectsWith(bounds)))
        {
            return false;
        }

        // Only non-cloaked windows are actually visible (exclusive-fullscreen games cloak the desktop).
        // Explorer's desktop/wallpaper host windows (WorkerW, Progman, ...) report as visible and span
        // the whole screen but never occlude anything. Fully transparent layered overlays (game
        // overlays etc.) span the whole screen but cannot be seen either.
        if (IsCloaked(hwnd) || IsDesktopShellWindow(hwnd) || IsFullyTransparent(hwnd))
        {
            return false;
        }

        occluderRect = bounds;
        return true;
    }

    /// <summary>
    /// Computes how much of <paramref name="target"/> (in virtual screen coordinates) is covered by
    /// <paramref name="occluders"/>.
    /// </summary>
    private static double ComputeCoverage(Rectangle target, List<Rectangle> occluders)
    {
        List<Rectangle> coveringRects = [];

        foreach (Rectangle occluder in occluders)
        {
            Rectangle intersection = Rectangle.Intersect(target, occluder);
            if (intersection.Width > 0 && intersection.Height > 0)
            {
                coveringRects.Add(intersection);
            }
        }

        if (coveringRects.Count == 0 || target.Width <= 0 || target.Height <= 0)
        {
            return 0;
        }

        return UnionArea(coveringRects) / (double)(target.Width * target.Height);
    }

    private static bool IsForegroundCovering(Rectangle target, nint selfHwnd)
    {
        nint foreground = W32.GetForegroundWindow();
        if (foreground == nint.Zero || foreground == selfHwnd)
        {
            return false;
        }

        if (W32.GetWindowThreadProcessId(foreground, out _) == 0)
        {
            return false;
        }

        // A maximized/fullscreen app from another process covering the widget pauses it.
        _ = W32.GetWindowRect(foreground, out W32.RECT rect);
        Rectangle intersection = Rectangle.Intersect(target, rect);

        return target.Width > 0 && target.Height > 0
            && intersection.Width >= target.Width * CoverageThreshold
            && intersection.Height >= target.Height * CoverageThreshold;
    }

    private static bool IsCloaked(nint hwnd)
    {
        return W32.DwmGetWindowAttribute(hwnd, W32.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;
    }

    /// <summary>
    /// Returns true for layered windows that are fully transparent (alpha 0), i.e. invisible
    /// overlay hosts that span the screen but never visibly occlude anything.
    /// </summary>
    private static bool IsFullyTransparent(nint hwnd)
    {
        if ((W32.GetWindowLong(hwnd, W32.GWL_EXSTYLE) & W32.WS_EX_LAYERED) == 0)
        {
            return false;
        }

        return W32.GetLayeredWindowAttributes(hwnd, out _, out byte alpha, out int flags)
            && (flags & W32.LWA_ALPHA) != 0
            && alpha == 0;
    }

    /// <summary>
    /// Returns true for the Explorer desktop/wallpaper host windows that span the whole screen but
    /// never visually occlude anything (wallpaper, hidden desktop icon layer, ...).
    /// </summary>
    private static bool IsDesktopShellWindow(nint hwnd)
    {
        StringBuilder className = new(256);
        _ = W32.GetClassName(hwnd, className, className.Capacity);

        return className.ToString() is "WorkerW" or "Progman" or "SHELLDLL_DefView" or "SysListView32";
    }

    /// <summary>
    /// Computes the total (union) area of <paramref name="rects"/> using a y-scanline sweep so
    /// overlapping windows are not double counted.
    /// </summary>
    private static long UnionArea(List<Rectangle> rects)
    {
        if (rects.Count == 0)
        {
            return 0;
        }

        List<int> yEdges = [];
        foreach (Rectangle rect in rects)
        {
            yEdges.Add(rect.Top);
            yEdges.Add(rect.Bottom);
        }
        yEdges.Sort();

        long area = 0;
        for (int i = 0; i < yEdges.Count - 1; i++)
        {
            int y0 = yEdges[i];
            int y1 = yEdges[i + 1];
            if (y0 == y1)
            {
                continue;
            }

            // Collect the x extents of all rects that span this band, then merge them.
            List<(int Left, int Right)> xRanges = [];
            foreach (Rectangle rect in rects)
            {
                if (rect.Top <= y0 && rect.Bottom >= y1)
                {
                    xRanges.Add((rect.Left, rect.Right));
                }
            }

            if (xRanges.Count == 0)
            {
                continue;
            }

            xRanges.Sort((a, b) => a.Left.CompareTo(b.Left));
            int mergedLeft = xRanges[0].Left;
            int mergedRight = xRanges[0].Right;
            long bandWidth = 0;

            for (int j = 1; j < xRanges.Count; j++)
            {
                if (xRanges[j].Left <= mergedRight)
                {
                    mergedRight = Math.Max(mergedRight, xRanges[j].Right);
                }
                else
                {
                    bandWidth += mergedRight - mergedLeft;
                    mergedLeft = xRanges[j].Left;
                    mergedRight = xRanges[j].Right;
                }
            }
            bandWidth += mergedRight - mergedLeft;

            area += bandWidth * (y1 - y0);
        }

        return area;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        timer.Elapsed -= Timer_Elapsed;
        timer.Stop();
        timer.Dispose();
    }
}