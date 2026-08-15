using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace DesktopMagic.Helpers;

/// <summary>
/// Helpers for enumerating screens and converting between percentage based
/// positions/sizes (relative to a screen's bounds) and absolute positions.
/// All conversions work in WPF DIP space because WPF window <see cref="Window.Left"/>,
/// <see cref="Window.Top"/>, <see cref="Window.Width"/> and <see cref="Window.Height"/>
/// are expressed in device independent pixels, while <see cref="System.Windows.Forms.Screen.Bounds"/>
/// is expressed in physical pixels. Each screen is therefore converted to DIP space
/// using its own DPI scaling factor.
/// </summary>
public static class ScreenUtilities
{
    private const uint MonitorDefaultToNearest = 0x00000002;

    private const int MDT_EFFECTIVE_DPI = 0;

    /// <summary>
    /// Gets the list of all screens currently connected, in the order reported by Windows.
    /// </summary>
    public static List<System.Windows.Forms.Screen> GetAllScreens()
    {
        return System.Windows.Forms.Screen.AllScreens.ToList();
    }

    /// <summary>
    /// Gets the screen matching the given device name (e.g. "\\.\DISPLAY1"), or null.
    /// </summary>
    public static System.Windows.Forms.Screen? GetScreenByDeviceName(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }

        return System.Windows.Forms.Screen.AllScreens.FirstOrDefault(screen => string.Equals(screen.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the primary screen, or the first available screen as a fallback.
    /// </summary>
    public static System.Windows.Forms.Screen GetPrimaryScreen()
    {
        return System.Windows.Forms.Screen.PrimaryScreen ?? System.Windows.Forms.Screen.AllScreens.FirstOrDefault()
            ?? throw new InvalidOperationException("No screens detected.");
    }

    /// <summary>
    /// Aspect ratio (Width / Height) of the screen's bounds.
    /// </summary>
    public static double GetAspectRatio(System.Windows.Forms.Screen screen)
    {
        if (screen.Bounds.Height == 0)
        {
            return 0;
        }

        return screen.Bounds.Width / (double)screen.Bounds.Height;
    }

    /// <summary>
    /// Converts a percentage based position (0..1 relative to the screen bounds) to an absolute
    /// WPF position (DIPs) on that screen.
    /// </summary>
    public static Point PercentToPosition(Point percent, System.Drawing.Rectangle bounds)
    {
        Rect dips = GetScreenDips(bounds);
        return new Point(
            dips.Left + (percent.X * dips.Width),
            dips.Top + (percent.Y * dips.Height));
    }

    /// <summary>
    /// Converts an absolute WPF position (DIPs) to a percentage (0..1) of the screen bounds.
    /// </summary>
    public static Point PositionToPercent(Point position, System.Drawing.Rectangle bounds)
    {
        Rect dips = GetScreenDips(bounds);
        if (dips.Width == 0 || dips.Height == 0)
        {
            return new Point(0.05, 0.05);
        }

        return new Point(
            (position.X - dips.Left) / dips.Width,
            (position.Y - dips.Top) / dips.Height);
    }

    /// <summary>
    /// Converts a percentage based size (0..1 of the screen bounds) to an absolute WPF size (DIPs).
    /// </summary>
    public static Point PercentSizeToSize(Point percent, System.Drawing.Rectangle bounds)
    {
        Rect dips = GetScreenDips(bounds);
        return new Point(
            percent.X * dips.Width,
            percent.Y * dips.Height);
    }

    /// <summary>
    /// Converts an absolute WPF size (DIPs) to a percentage (0..1) of the screen bounds.
    /// </summary>
    public static Point SizeToPercent(Point size, System.Drawing.Rectangle bounds)
    {
        Rect dips = GetScreenDips(bounds);
        if (dips.Width == 0 || dips.Height == 0)
        {
            return new Point(0.3, 0.3);
        }

        return new Point(
            size.X / dips.Width,
            size.Y / dips.Height);
    }

    /// <summary>
    /// Clamps an absolute WPF position (DIPs) so that the window (of the given size)
    /// stays within the screen bounds. This prevents widgets from being moved to another screen.
    /// </summary>
    public static Point ClampToScreenBounds(Point topLeft, Size size, System.Drawing.Rectangle bounds)
    {
        Rect dips = GetScreenDips(bounds);
        double x = topLeft.X;
        double y = topLeft.Y;

        if (size.Width <= dips.Width)
        {
            x = Math.Clamp(x, dips.Left, dips.Right - size.Width);
        }
        else
        {
            x = dips.Left;
        }

        if (size.Height <= dips.Height)
        {
            y = Math.Clamp(y, dips.Top, dips.Bottom - size.Height);
        }
        else
        {
            y = dips.Top;
        }

        return new Point(x, y);
    }

    /// <summary>
    /// Builds a human readable label for a screen, e.g. "Display 1 · DELL U2715H · 3840x2160".
    /// </summary>
    public static string GetScreenLabel(System.Windows.Forms.Screen screen, int index)
    {
        string name = GetFriendlyName(screen);
        string resolution = $"{screen.Bounds.Width}x{screen.Bounds.Height}";

        return string.IsNullOrWhiteSpace(name) || string.Equals(name, "Generic PnP Monitor", StringComparison.OrdinalIgnoreCase)
            ? $"Display {index + 1} · {resolution}"
            : $"Display {index + 1} · {name} · {resolution}";
    }

    /// <summary>
    /// Gets the friendly monitor model name (e.g. "DELL U2715H") for the given screen,
    /// or an empty string when it cannot be determined.
    /// </summary>
    public static string GetFriendlyName(System.Windows.Forms.Screen screen)
    {
        return GetMonitorDisplayDevice(screen)?.DeviceString ?? string.Empty;
    }

    /// <summary>
    /// Gets a stable hardware identifier for the screen: the PnP device instance ID of its
    /// monitor (e.g. "MONITOR\DEL41F1\{...}\{0001}"), which is derived from the monitor's
    /// EDID and survives disconnects and reconnects of the same monitor. When no hardware ID
    /// is available (e.g. virtual or remote displays), a deterministic SHA-256 hash of the
    /// bounds is used so the identifier is never empty.
    /// </summary>
    public static string GetMonitorHardwareId(System.Windows.Forms.Screen screen)
    {
        string? deviceId = GetMonitorDisplayDevice(screen)?.DeviceID;
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        System.Drawing.Rectangle bounds = screen.Bounds;
        string boundsString = $"{bounds.X}-{bounds.Y}-{bounds.Width}-{bounds.Height}";
        string hashString = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(boundsString))).ToLowerInvariant();
        return $"DISPLAY#{hashString}";
    }

    /// <summary>
    /// Gets the monitor device info (second-level <see cref="EnumDisplayDevices"/> entry) for
    /// the given screen, or null when it cannot be determined.
    /// </summary>
    private static DISPLAY_DEVICE? GetMonitorDisplayDevice(System.Windows.Forms.Screen screen)
    {
        try
        {
            DISPLAY_DEVICE device = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICE>() };
            if (EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
            {
                DISPLAY_DEVICE monitor = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf<DISPLAY_DEVICE>() };
                if (EnumDisplayDevices(device.DeviceName, 0, ref monitor, 0))
                {
                    return monitor;
                }
            }
        }
        catch (Exception)
        {
            // Fall through to null.
        }

        return null;
    }

    /// <summary>
    /// Enumerates monitors in a Per-Monitor V2 DPI awareness context and returns each
    /// monitor's bounds in physical (device) pixels on the virtual screen, keyed by device
    /// name (e.g. "\\.\DISPLAY1"). This uses the same coordinate space as DPI-aware apps
    /// such as Lively Wallpaper, keeping multi-screen layouts with mixed DPI scaling
    /// consistent regardless of this app's own DPI awareness mode.
    /// </summary>
    public static Dictionary<string, System.Drawing.Rectangle> GetDisplayPhysicalBounds()
    {
        Dictionary<string, System.Drawing.Rectangle> result = new Dictionary<string, System.Drawing.Rectangle>();

        IntPtr prevContext = IntPtr.Zero;
        bool contextChanged = false;
        try
        {
            prevContext = SetThreadDpiAwarenessContext((IntPtr)DpiAwarenessContextPerMonitorV2);
            contextChanged = prevContext != IntPtr.Zero;
        }
        catch (Exception)
        {
            // SetThreadDpiAwarenessContext unavailable; fall back to default context.
        }

        try
        {
            _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                MONITORINFOEX info = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMonitor, ref info))
                {
                    string deviceName = info.szDevice.TrimEnd('\0');
                    result[deviceName] = new System.Drawing.Rectangle(
                        info.rcMonitor.Left, info.rcMonitor.Top,
                        info.rcMonitor.Right - info.rcMonitor.Left,
                        info.rcMonitor.Bottom - info.rcMonitor.Top);
                }

                return true;
            }, IntPtr.Zero);
        }
        finally
        {
            if (contextChanged)
            {
                _ = SetThreadDpiAwarenessContext(prevContext);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the screen bounds converted to WPF DIP space using the screen's own DPI scaling factor.
    /// </summary>
    private static Rect GetScreenDips(System.Drawing.Rectangle bounds)
    {
        double scale = GetDpiScale(bounds);
        return new Rect(bounds.Left / scale, bounds.Top / scale, bounds.Width / scale, bounds.Height / scale);
    }

    /// <summary>
    /// Gets the DPI scaling factor (relative to 96 DPI) of the screen containing the given bounds.
    /// </summary>
    private static double GetDpiScale(System.Drawing.Rectangle bounds)
    {
        try
        {
            POINT center = new POINT
            {
                X = bounds.Left + (bounds.Width / 2),
                Y = bounds.Top + (bounds.Height / 2)
            };
            IntPtr monitor = MonitorFromPoint(center, MonitorDefaultToNearest);
            if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0)
            {
                return dpiX / 96.0;
            }
        }
        catch (Exception)
        {
            // Fall through to no scaling if DPI APIs are unavailable.
        }

        return 1.0;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT point, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public uint cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

        public uint StateFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const long DpiAwarenessContextPerMonitorV2 = -4;

    private delegate bool EnumMonitorsProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORRECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public uint cbSize;

        public MONITORRECT rcMonitor;

        public MONITORRECT rcWork;

        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
