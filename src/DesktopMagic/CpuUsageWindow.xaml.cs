using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopMagic
{
    /// <summary>
    /// Interaktionslogik für TimeWindow.xaml
    /// </summary>
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
            this.Owner = w;
            w.Hide();

            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            Timer t = new Timer();
            t.Interval = 100;
            t.Elapsed += T_Elapsed;
            t.Start();

            Timer valueTimer = new Timer();
            valueTimer.Interval = 1000;
            valueTimer.Elapsed += ValueTimer_Elapsed; ;
            valueTimer.Start();

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + MainWindow.AppName);
            this.Top = double.Parse(key.GetValue("CpuUsageWindowTop", 100).ToString());
            this.Left = double.Parse(key.GetValue("CpuUsageWindowLeft", 100).ToString());
            this.Height = double.Parse(key.GetValue("CpuUsageWindowHeight", 200).ToString());
            this.Width = double.Parse(key.GetValue("CpuUsageWindowWidth", 500).ToString());
            this.IsEnabled = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new WindowInteropHelper(this);
            WindowPos.SetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE,
            WindowPos.GetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE) | WindowPos.WS_EX_NOACTIVATE);
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWindow.EditMode)
                {
                    panel.Visibility = Visibility.Visible;
                    WindowPos.SetIsLocked(this, false);
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    WindowPos.SetIsLocked(this, true);
                }

                textBlock.FontFamily = new FontFamily(MainWindow.GlobalFont);
                textBlock.Foreground = MainWindow.GlobalColor;
            });
        }

        private void ValueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string cpuUsage = GetCpuUsage();
            Dispatcher.Invoke(() =>
            {
                textBlock.Text = cpuUsage;
            });
        }

        private string GetCpuUsage()
        {
            return "CPU: " + ((int)cpuCounter.NextValue()).ToString().PadLeft(3, '0') + "%";
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            key.SetValue("CpuUsageWindowTop", this.Top);
            key.SetValue("CpuUsageWindowLeft", this.Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue("CpuUsageWindowHeight", this.Height);
            key.SetValue("CpuUsageWindowWidth", this.Width);
            tileBar.CaptionHeight = this.ActualHeight - 10;
        }
    }
}