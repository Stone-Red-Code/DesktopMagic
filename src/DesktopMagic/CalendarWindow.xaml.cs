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
    /// <summary>
    /// Interaktionslogik für TimeWindow.xaml
    /// </summary>
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
            this.Owner = w;
            w.Hide();

            Timer t = new Timer();
            t.Interval = 100;
            t.Elapsed += T_Elapsed;
            t.Start();

            Timer valueTimer = new Timer();
            valueTimer.Interval = 600000;
            valueTimer.Elapsed += ValueTimer_Elapsed; ;
            valueTimer.Start();

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + MainWindow.AppName);
            this.Top = double.Parse(key.GetValue("CalendarWindowTop", 100).ToString());
            this.Left = double.Parse(key.GetValue("CalendarWindowLeft", 100).ToString());
            this.Height = double.Parse(key.GetValue("CalendarWindowHeight", 200).ToString());
            this.Width = double.Parse(key.GetValue("CalendarWindowWidth", 500).ToString());
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
            var helper = new WindowInteropHelper(this);
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

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWindow.EditMode)
                {
                    panel.Visibility = Visibility.Visible;
                    new WindowPos().SetIsLocked(this, false);
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    new WindowPos().SetIsLocked(this, true);
                }
                if (MainWindow.GlobalFont != oldFont || MainWindow.GlobalColor != oldColor)
                {
                    oldColor = MainWindow.GlobalColor;
                    oldFont = MainWindow.GlobalFont;
                    LoadEvents();
                }
                listBox.SelectedIndex = -1;
            });
        }

        private void LoadEvents()
        {
            listBox.Items.Clear();
            CalendarItems calendarItem = new CalendarItems();
            calendarItem.eventname = "Termine:             ";
            calendarItem.font = MainWindow.GlobalFont;
            calendarItem.color = MainWindow.GlobalColor.ToString();
            listBox.Items.Add(calendarItem);

            for (int i = 0; i < upcomingEventNames.Count; i++)
            {
                DateTime dateTime = DateTime.Now;
                DateTime.TryParse(upcomingEventTimes[i], out dateTime);

                calendarItem = new CalendarItems();
                calendarItem.eventname = upcomingEventNames[i];
                calendarItem.dateTime = dateTime.ToString("dd-MM-yyyy");
                calendarItem.font = MainWindow.GlobalFont;
                calendarItem.color = MainWindow.GlobalColor.ToString();

                if (dateTime < DateTime.Today.AddMonths(12) || upcomingEventTimes[i] == "-")
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
            key.SetValue("CalendarWindowTop", this.Top);
            key.SetValue("CalendarWindowLeft", this.Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue("CalendarWindowHeight", this.Height);
            key.SetValue("CalendarWindowWidth", this.Width);
            tileBar.CaptionHeight = this.ActualHeight - 10;
        }
    }
}