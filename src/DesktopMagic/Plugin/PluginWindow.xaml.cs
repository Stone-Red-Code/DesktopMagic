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
using System.Windows.Media;
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

        public event Action PluginLoaded;

        public event Action OnExit;

        public PluginWindow(string pluginName)
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

            this.pluginName = pluginName;

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
                    tileBar.CaptionHeight = tileBar.CaptionHeight = this.ActualHeight - 10 < 0 ? 0 : this.ActualHeight - 10;
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
            string PluginPath;

            pluginFolderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{MainWindow.AppName}\\Plugins\\{pluginName}";

            if (File.Exists($"{pluginFolderPath}\\{pluginName}.dll"))
            {
                PluginPath = $"{pluginFolderPath}\\{pluginName}.dll";
            }
            else
            {
                _ = MessageBox.Show("File does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }

            try
            {
                ExecuteSource();
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Log(ex.ToString(), "Plugin");
                _ = MessageBox.Show("File execution error:\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }
            PluginLoaded?.Invoke();
        }

        private void ExecuteSource()
        {
            byte[] assemblyBytes = File.ReadAllBytes($"{pluginFolderPath}\\{pluginName}.dll");
            Assembly dll = Assembly.Load(assemblyBytes);
            Type instanceType = dll.GetTypes().FirstOrDefault(type => type.GetTypeInfo().BaseType == typeof(Plugin));

            foreach (var item in dll.GetTypes())
            {
                Debug.WriteLine(item.Name);
                Debug.WriteLine(typeof(Plugin).Name);
            }

            if (instanceType is null)
            {
                _ = MessageBox.Show($"The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
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
                _ = MessageBox.Show($"The \"Plugin\" class has to inherit from \"{typeof(Plugin).FullName}\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }

            LoadOptions(instance);

            valueTimer = new System.Timers.Timer();
            valueTimer.Interval = 1000;
            valueTimer.Elapsed += ValueTimer_Elapsed;

            pluginClassInstance.Start();
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
            FieldInfo[] props = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);

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
            try
            {
                Bitmap result = pluginClassInstance.Main();

                if (pluginClassInstance.UpdateInterval > 0)
                {
                    valueTimer.Interval = pluginClassInstance.UpdateInterval;
                }
                else
                {
                    valueTimer.Stop();
                }

                //Update Image
                Dispatcher.Invoke(() =>
                {
                    image.Source = BitmapToImageSource(result);
                });
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Log(ex.ToString(), "Plugin");
                _ = MessageBox.Show("File execution error:\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }

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

        private void Exit()
        {
            Dispatcher.Invoke(() =>
            {
                OnExit?.Invoke();
            });
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
            ImageSource imageSource = image.Source;
            BitmapSource bitmapImage = (BitmapSource)imageSource;
            double pixelMousePositionX = e.GetPosition(image).X * bitmapImage.PixelWidth / image.ActualHeight;
            double pixelMousePositionY = e.GetPosition(image).Y * bitmapImage.PixelHeight / image.ActualHeight;

            MouseButton mouseButton;

            switch (e.ChangedButton)
            {
                case System.Windows.Input.MouseButton.Left:
                    mouseButton = MouseButton.Left;
                    break;

                case System.Windows.Input.MouseButton.Middle:
                    mouseButton = MouseButton.Middle;
                    break;

                case System.Windows.Input.MouseButton.Right:
                    mouseButton = MouseButton.Right;
                    break;

                default: return;
            };

            System.Drawing.Point point = new System.Drawing.Point((int)pixelMousePositionX, (int)pixelMousePositionY);
            pluginClassInstance.OnMouseClick(point, mouseButton);
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ImageSource imageSource = image.Source;
            BitmapSource bitmapImage = (BitmapSource)imageSource;
            double pixelMousePositionX = e.GetPosition(image).X * bitmapImage.PixelWidth / image.ActualHeight;
            double pixelMousePositionY = e.GetPosition(image).Y * bitmapImage.PixelHeight / image.ActualHeight;

            System.Drawing.Point point = new System.Drawing.Point((int)pixelMousePositionX, (int)pixelMousePositionY);
            pluginClassInstance.OnMouseMove(point);
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ImageSource imageSource = image.Source;
            BitmapSource bitmapImage = (BitmapSource)imageSource;
            double pixelMousePositionX = e.GetPosition(image).X * bitmapImage.PixelWidth / image.ActualHeight;
            double pixelMousePositionY = e.GetPosition(image).Y * bitmapImage.PixelHeight / image.ActualHeight;

            System.Drawing.Point point = new System.Drawing.Point((int)pixelMousePositionX, (int)pixelMousePositionY);
            pluginClassInstance.OnMouseWheel(point, e.Delta);
        }

        #endregion Window Events
    }
}