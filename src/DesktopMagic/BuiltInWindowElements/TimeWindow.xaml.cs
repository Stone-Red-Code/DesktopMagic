﻿using DesktopMagic.Helpers;

using Microsoft.Win32;

using System;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopMagic
{
    public partial class TimeWindow : Window
    {
        private RegistryKey key;

        public TimeWindow()
        {
            InitializeComponent();

            Window w = new Window();
            w.Top = -100;
            w.Left = -100;
            w.Width = 0;
            w.Height = 0;

            w.WindowStyle = WindowStyle.ToolWindow;
            w.ShowInTaskbar = false;
            w.Show();
            Owner = w;
            w.Hide();

            Timer t = new Timer();
            t.Interval = 100;
            t.Elapsed += UpdateTimer_Elapsed;
            t.Start();

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + App.AppName);
            Top = double.Parse(key.GetValue("TimeWindowTop", 100).ToString());
            Left = double.Parse(key.GetValue("TimeWindowLeft", 100).ToString());
            Height = double.Parse(key.GetValue("TimeWindowHeight", 200).ToString());
            Width = double.Parse(key.GetValue("TimeWindowWidth", 500).ToString());
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
                //textBlock.Text = DateTime.Now.ToString("hh:mm:ss tt");
                textBlock.Text = DateTime.Now.ToString("HH:mm:ss");

                ClculateWidth();
            });
        }

        private void ClculateWidth()
        {
            string template = "##:##:##";
            double lenght = 0;
            for (int i = 0; i < 9; i++)
            {
                lenght = Math.Max(StringUtilities.MeasureString(template.Replace('#', i.ToString()[0]), textBlock).Width, lenght);
            }
            textBlock.Width = lenght;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            key.SetValue("TimeWindowTop", Top);
            key.SetValue("TimeWindowLeft", Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue("TimeWindowHeight", Height);
            key.SetValue("TimeWindowWidth", Width);
            tileBar.CaptionHeight = ActualHeight - 10;
        }
    }
}