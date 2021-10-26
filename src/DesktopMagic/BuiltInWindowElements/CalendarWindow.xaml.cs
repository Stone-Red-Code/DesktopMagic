using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopMagic
{
    [Obsolete("Disabled due to lack of support (too much work)")]
    public partial class CalendarWindow : Window
    {
        private RegistryKey key;
        private string oldFont = "";
        private Brush oldColor;
        private List<string> upcomingEventNames = new List<string>();
        private List<string> upcomingEventTimes = new List<string>();

        public CalendarWindow()
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

            Timer valueTimer = new Timer();
            valueTimer.Interval = 600000;
            valueTimer.Elapsed += ValueTimer_Elapsed;
            ;
            valueTimer.Start();

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + App.AppName);
            Top = double.Parse(key.GetValue("CalendarWindowTop", 100).ToString());
            Left = double.Parse(key.GetValue("CalendarWindowLeft", 100).ToString());
            Height = double.Parse(key.GetValue("CalendarWindowHeight", 200).ToString());
            Width = double.Parse(key.GetValue("CalendarWindowWidth", 500).ToString());
            //this.IsEnabled = false;

            Task.Run(() =>
            {
                (upcomingEventNames, upcomingEventTimes) = new CalendarManagment().GetEvents();
                Dispatcher.Invoke(() =>
                {
                    LoadEvents();
                });
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new WindowInteropHelper(this);
            WindowPos.SetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE,
            WindowPos.GetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE) | WindowPos.WS_EX_NOACTIVATE);
        }

        private void ValueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                (upcomingEventNames, upcomingEventTimes) = new CalendarManagment().GetEvents();
                Dispatcher.Invoke(() =>
                {
                    LoadEvents();
                });
            });
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
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
                if (MainWindow.Theme.Font != oldFont || MainWindow.Theme.PrimaryBrush != oldColor)
                {
                    oldColor = MainWindow.Theme.PrimaryBrush;
                    oldFont = MainWindow.Theme.Font;
                    LoadEvents();
                }
                listBox.SelectedIndex = -1;
            });
        }

        private void LoadEvents()
        {
            listBox.Items.Clear();
            CalendarItems calendarItem = new CalendarItems();
            calendarItem.Eventname = "Termine:             ";
            calendarItem.Font = MainWindow.Theme.Font;
            calendarItem.Color = MainWindow.Theme.PrimaryColor.ToString();
            listBox.Items.Add(calendarItem);

            for (int i = 0; i < upcomingEventNames.Count; i++)
            {
                calendarItem = new CalendarItems();
                calendarItem.Eventname = upcomingEventNames[i];
                calendarItem.DateTime = DateTime.Now.ToString("dd-MM-yyyy");
                calendarItem.Font = MainWindow.Theme.Font;
                calendarItem.Color = MainWindow.Theme.PrimaryBrush.ToString();

                if (DateTime.Now < DateTime.Today.AddMonths(12) || upcomingEventTimes[i] == "-")
                {
                    listBox.Items.Add(calendarItem);
                }
            }

            while (listBox.Items.Count < 10)
            {
                listBox.Items.Add("");
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            key.SetValue("CalendarWindowTop", Top);
            key.SetValue("CalendarWindowLeft", Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue("CalendarWindowHeight", Height);
            key.SetValue("CalendarWindowWidth", Width);
            tileBar.CaptionHeight = ActualHeight - 10;
        }
    }
}