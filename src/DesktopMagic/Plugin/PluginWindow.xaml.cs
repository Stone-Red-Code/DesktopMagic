using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Inputs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DesktopMagic
{
    /// <summary>
    /// Interaktionslogik für PluginWindow.xaml
    /// </summary>
    ///
    public partial class PluginWindow : Window
    {
        private readonly RegistryKey key;
        private Thread pluginThread;
        private System.Timers.Timer valueTimer;

        private Plugin pluginClassInstance;
        private readonly string pluginName = "";
        private string pluginFolderPath = "";
        private bool stop = false;

        public PluginWindow(string _PluginName)
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

            System.Timers.Timer t = new System.Timers.Timer();
            t.Interval = 100;
            t.Elapsed += Elapsed;
            t.Start();

            pluginName = _PluginName;

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + MainWindow.AppName);
            this.Top = double.Parse(key.GetValue(pluginName + "WindowTop", 100).ToString());
            this.Left = double.Parse(key.GetValue(pluginName + "WindowLeft", 100).ToString());
            this.Height = double.Parse(key.GetValue(pluginName + "WindowHeight", 200).ToString());
            this.Width = double.Parse(key.GetValue(pluginName + "WindowWidth", 500).ToString());
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new(this);
            WindowPos.SetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE,
            WindowPos.GetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE) | WindowPos.WS_EX_NOACTIVATE);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            pluginThread = new Thread(() =>
            {
                LoadPlugin();
            });
            pluginThread.Start();
        }

        public void UpdatePluginWindow() => ValueTimer_Elapsed(valueTimer, null);

        private void Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWindow.EditMode)
                {
                    panel.Visibility = Visibility.Visible;
                    new WindowPos().SetIsLocked(this, false);
                    tileBar.CaptionHeight = this.ActualHeight - 10;
                    this.ResizeMode = ResizeMode.CanResize;
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    new WindowPos().SetIsLocked(this, true);
                    tileBar.CaptionHeight = 0;
                    this.ResizeMode = ResizeMode.NoResize;
                }
                if (stop)
                {
                    ((System.Timers.Timer)sender).Stop();
                }
            });
        }

        private void LoadPlugin()
        {
            string sourceText;
            string PluginPath;

            pluginFolderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{MainWindow.AppName}\\Plugins\\{pluginName}";

            if (File.Exists($"{pluginFolderPath}\\{pluginName}.dll"))
            {
                PluginPath = $"{pluginFolderPath}\\{pluginName}.dll";
            }
            else
            {
                MessageBox.Show("File does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                sourceText = File.ReadAllText(PluginPath);
            }
            catch (Exception e)
            {
                MessageBox.Show("File could not be read:\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                ExecuteSource(sourceText);
            }
            catch (Exception e)
            {
                MessageBox.Show("File execution error:\n" + e, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void ExecuteSource(string sourceText)
        {
            byte[] assemblyBytes = File.ReadAllBytes($"{pluginFolderPath}\\{pluginName}.dll");
            Assembly dll = Assembly.Load(assemblyBytes);
            Type instanceType = dll.GetTypes().FirstOrDefault(type => type.Name == "PluginScript");

            if (instanceType is null)
            {
                MessageBox.Show($"The \"PluginScript\" class could not be found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            object instance = Activator.CreateInstance(instanceType);
            if (instance is Plugin)
            {
                pluginClassInstance = instance as Plugin;
                pluginClassInstance.Application = new PluginData(this);
            }
            else
            {
                MessageBox.Show($"The \"PluginScript\" class has to inherit from \"{nameof(DesktopMagicPluginAPI)}.{nameof(Plugin)}\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (instanceType.Namespace == nameof(DesktopMagic))
            {
                MessageBox.Show("NO!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadOptions(instance);

            valueTimer = new System.Timers.Timer();
            valueTimer.Interval = 1000;
            valueTimer.Elapsed += ValueTimer_Elapsed;

            UpdatePluginWindow();

            if (pluginClassInstance.UpdateInterval > 0)
            {
                valueTimer.Interval = pluginClassInstance.UpdateInterval;
                valueTimer.Start();
            }
        }

        private void LoadOptions(object instance)
        {
            Debug.WriteLine(instance.GetType().FullName);
            FieldInfo[] props = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            List<SettingElement> settingElements = new List<SettingElement>();
            foreach (FieldInfo prop in props)
            {
                if (prop.GetValue(instance) is Element element)
                {
                    object[] attrs = prop.GetCustomAttributes(true);
                    foreach (object attr in attrs)
                    {
                        if (attr is ElementAttribute elementAttribute)
                        {
                            settingElements.Add(new SettingElement(element, elementAttribute.Name, elementAttribute.OrderIndex));
                            break;
                        }
                    }
                }
            }

            settingElements = settingElements.OrderBy(x => x.OrderIndex).ToList();
            if (MainWindow.PluginsSettings.ContainsKey(pluginName))
            {
                MainWindow.PluginsSettings[pluginName] = settingElements;
            }
            else
            {
                MainWindow.PluginsSettings.Add(pluginName, settingElements);
            }
        }

        private void ValueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Set Arguments
            SolidBrush newBrush = (SolidBrush)MainWindow.GlobalSystemColor;
            Color color = newBrush.Color;
            string font = MainWindow.GlobalFont;

            Bitmap result = pluginClassInstance.Main();

            //Update Image
            Dispatcher.Invoke(() =>
            {
                image.Source = BitmapToImageSource(result);
            });

            if (stop)
            {
                valueTimer.Stop();
            }
        }

        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            BitmapSource bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgra32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        #region Window Events

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            key.SetValue(pluginName + "WindowTop", this.Top);
            key.SetValue(pluginName + "WindowLeft", this.Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue(pluginName + "WindowHeight", this.Height);
            key.SetValue(pluginName + "WindowWidth", this.Width);
            tileBar.CaptionHeight = this.ActualHeight - 10;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stop = true;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clicked(new System.Drawing.Point((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y));
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Moved(new System.Drawing.Point((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y));
        }

        #endregion Window Events

        #region Plugin Methods

        private void Clicked(System.Drawing.Point positon)
        {
            pluginClassInstance.OnMouseClick(positon);
        }

        private void Moved(System.Drawing.Point positon)
        {
            pluginClassInstance.OnMouseMove(positon);
        }

        #endregion Plugin Methods
    }
}