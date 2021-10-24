using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace DesktopMagic
{
    using static NativeMethods;

    public class WindowPos : DependencyObject
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

        private const UInt32 SWP_NOSIZE1 = 0x0001;
        private const UInt32 SWP_NOMOVE1 = 0x0002;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd, out Rectangle rect);

        public static void SendWpfWindowBack(Window window)
        {
            var hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE1 | SWP_NOMOVE1);
        }

        public static bool GetIsLocked(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLockedProperty);
        }

        public static void SetIsLocked(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLockedProperty, value);
        }

        public static readonly DependencyProperty IsLockedProperty =
            DependencyProperty.RegisterAttached("IsLocked", typeof(bool), typeof(WindowPos),
                new PropertyMetadata(false, IsLocked_Changed));

        private static void IsLocked_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (Window)d;
            var isHooked = d.GetValue(IsHookedProperty) != null;

            if (!isHooked)
            {
                var hook = new WindowLockHook(window);
                d.SetValue(IsHookedProperty, hook);
            }
        }

        private static readonly DependencyProperty IsHookedProperty =
            DependencyProperty.RegisterAttached("IsHooked", typeof(WindowLockHook), typeof(WindowPos),
                new PropertyMetadata(null));

        private class WindowLockHook
        {
            private readonly Window Window;

            public WindowLockHook(Window window)
            {
                this.Window = window;

                var source = PresentationSource.FromVisual(window) as HwndSource;
                if (source == null)
                {
                    // If there is no hWnd, we need to wait until there is
                    window.SourceInitialized += Window_SourceInitialized;
                }
                else
                {
                    source.AddHook(WndProc);
                }
            }

            private void Window_SourceInitialized(object sender, EventArgs e)
            {
                var source = (HwndSource)PresentationSource.FromVisual(Window);
                source.AddHook(WndProc);
            }

            public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_WINDOWPOSCHANGING && GetIsLocked(Window))
                {
                    var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    wp.flags |= SWP_NOMOVE | SWP_NOSIZE;
                    Marshal.StructureToPtr(wp, lParam, false);
                }

                return IntPtr.Zero;
            }
        }
    }

    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        public const int
            SWP_NOMOVE = 0x0002,
            SWP_NOSIZE = 0x0001;

        public const int
            WM_WINDOWPOSCHANGING = 0x0046;
    }
}