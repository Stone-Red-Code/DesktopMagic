using DesktopMagic.Helpers;

using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopMagic
{
    public partial class CpuUsageWindow : Window
    {
        private RegistryKey key;
        private PerformanceCounter cpuCounter = new PerformanceCounter();

        public CpuUsageWindow()
        {
            InitializeComponent();

            Window w = new()
            {
                Top = -100,
                Left = -100,
                Width = 0,
                Height = 0,

                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false
            };
            w.Show();
            Owner = w;
            w.Hide();

            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            Timer t = new Timer();
            t.Interval = 100;
            t.Elapsed += UpdateTimer_Elapsed;
            t.Start();

            Timer valueTimer = new Timer();
            valueTimer.Interval = 1000;
            valueTimer.Elapsed += ValueTimer_Elapsed;
            ;
            valueTimer.Start();

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + App.AppName);
            Top = double.Parse(key.GetValue("CpuUsageWindowTop", 100).ToString());
            Left = double.Parse(key.GetValue("CpuUsageWindowLeft", 100).ToString());
            Height = double.Parse(key.GetValue("CpuUsageWindowHeight", 200).ToString());
            Width = double.Parse(key.GetValue("CpuUsageWindowWidth", 500).ToString());
            IsEnabled = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new WindowInteropHelper(this);
            WindowPos.SetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE,
            WindowPos.GetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE) | WindowPos.WS_EX_NOACTIVATE);
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWindow.EditMode)
                {
                    panel.Visibility = Visibility.Visible;
                    tileBar.CaptionHeight = tileBar.CaptionHeight = ActualHeight - 10 < 0 ? 0 : ActualHeight - 10;
                    WindowPos.SetIsLocked(this, false);
                    ResizeMode = ResizeMode.CanResize;
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    tileBar.CaptionHeight = 0;
                    WindowPos.SetIsLocked(this, true);
                    ResizeMode = ResizeMode.NoResize;
                }

                rectangleGeometry.Rect = new Rect(0, 0, border.ActualWidth, border.ActualHeight);
                border.Background = MainWindow.Theme.BackgroundBrush;
                border.CornerRadius = new CornerRadius(MainWindow.Theme.CornerRadius);
                textBlock.FontFamily = new FontFamily(MainWindow.Theme.Font);
                textBlock.Foreground = MainWindow.Theme.PrimaryBrush;
                valueTextBlock.FontFamily = new FontFamily(MainWindow.Theme.Font);
                valueTextBlock.Foreground = MainWindow.Theme.PrimaryBrush;

                ClculateWidth();
            });
        }

        private void ValueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string cpuUsage = GetCpuUsage();
            Dispatcher.Invoke(() =>
            {
                valueTextBlock.Text = cpuUsage;
            });
        }

        private void ClculateWidth()
        {
            string template = "CPU: ###%";
            double lenght = 0;
            for (int i = 0; i < 9; i++)
            {
                lenght = Math.Max(StringUtilities.MeasureString(template.Replace('#', i.ToString()[0]), textBlock).Width, lenght);
            }
            dockPanel.Width = lenght;
        }

        private string GetCpuUsage()
        {
            return $"{(int)cpuCounter.NextValue()}%";
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            key.SetValue("CpuUsageWindowTop", Top);
            key.SetValue("CpuUsageWindowLeft", Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue("CpuUsageWindowHeight", Height);
            key.SetValue("CpuUsageWindowWidth", Width);
            tileBar.CaptionHeight = ActualHeight - 10;
        }
    }
}