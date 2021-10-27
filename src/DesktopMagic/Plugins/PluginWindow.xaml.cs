using DesktopMagic.Plugins;

using DesktopMagicPluginAPI;
using DesktopMagicPluginAPI.Drawing;
using DesktopMagicPluginAPI.Inputs;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
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
    public partial class PluginWindow : Window
    {
        private readonly RegistryKey key;
        private Thread pluginThread;
        private System.Timers.Timer valueTimer;
        private bool stop = false;

        private Plugin pluginClassInstance;
        public string PluginName { get; private set; }
        public string PluginFolderPath { get; private set; }

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
            Owner = w;
            w.Hide();

            System.Timers.Timer t = new System.Timers.Timer();
            t.Interval = 100;
            t.Elapsed += UpdateTimer_Elapsed;
            t.Start();

            PluginName = pluginName;

            key = Registry.CurrentUser.CreateSubKey(@"Software\" + App.AppName);
            Top = double.Parse(key.GetValue(pluginName + "WindowTop", 100).ToString());
            Left = double.Parse(key.GetValue(pluginName + "WindowLeft", 100).ToString());
            Height = double.Parse(key.GetValue(pluginName + "WindowHeight", 200).ToString());
            Width = double.Parse(key.GetValue(pluginName + "WindowWidth", 500).ToString());
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

        public void UpdatePluginWindow()
        {
            ValueTimer_Elapsed(valueTimer, null);
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWindow.EditMode)
                {
                    panel.Visibility = Visibility.Visible;
                    WindowPos.SetIsLocked(this, false);
                    tileBar.CaptionHeight = tileBar.CaptionHeight = ActualHeight - 10 < 0 ? 0 : ActualHeight - 10;
                    ResizeMode = ResizeMode.CanResize;
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    WindowPos.SetIsLocked(this, true);
                    tileBar.CaptionHeight = 0;
                    ResizeMode = ResizeMode.NoResize;
                }
                if (stop)
                {
                    ((System.Timers.Timer)sender).Stop();
                }
                else
                {
                    rectangleGeometry.Rect = new Rect(0, 0, border.ActualWidth, border.ActualHeight);
                    border.Background = MainWindow.Theme.BackgroundBrush;
                    border.CornerRadius = new CornerRadius(MainWindow.Theme.CornerRadius);
                }
            });
        }

        private void LoadPlugin()
        {
            PluginFolderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{App.AppName}\\Plugins\\{PluginName}";

            if (!File.Exists($"{PluginFolderPath}\\{PluginName}.dll"))
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
                App.Logger.Log(ex.ToString(), "Plugin");
                _ = MessageBox.Show("File execution error:\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }
            PluginLoaded?.Invoke();
        }

        private void ExecuteSource()
        {
            byte[] assemblyBytes = File.ReadAllBytes($"{PluginFolderPath}\\{PluginName}.dll");
            Assembly dll = Assembly.Load(assemblyBytes);
            Type instanceType = dll.GetTypes().FirstOrDefault(type => type.GetTypeInfo().BaseType == typeof(Plugin));

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
            try
            {
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
                if (MainWindow.PluginsSettings.ContainsKey(PluginName))
                {
                    MainWindow.PluginsSettings[PluginName] = settingElements;
                }
                else
                {
                    MainWindow.PluginsSettings.Add(PluginName, settingElements);
                }
            }
            catch (Exception ex)
            {
                stop = true;
                App.Logger.Log(ex.ToString(), "Plugin");
                _ = MessageBox.Show("File execution error:\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }
        }

        private void ValueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!stop)
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

                    if (result is not null)
                    {
                        BitmapScalingMode renderOptions = pluginClassInstance.RenderQuality switch
                        {
                            RenderQuality.High => BitmapScalingMode.HighQuality,
                            RenderQuality.Low => BitmapScalingMode.LowQuality,
                            RenderQuality.Performance => BitmapScalingMode.NearestNeighbor,
                            _ => BitmapScalingMode.Unspecified
                        };

                        //Update Image
                        Dispatcher.Invoke(() =>
                        {
                            RenderOptions.SetBitmapScalingMode(image, renderOptions);
                            image.Source = BitmapToImageSource(result);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                stop = true;
                App.Logger.Log(ex.ToString(), "Plugin");
                _ = MessageBox.Show("File execution error:\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
                return;
            }

            if (stop)
            {
                valueTimer.Stop();
            }
        }

        private static BitmapSource BitmapToImageSource(Bitmap bitmap)
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

        public void Exit()
        {
            stop = true;
            Dispatcher.Invoke(() =>
            {
                OnExit?.Invoke();
            });
        }

        #region Window Events

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            key.SetValue(PluginName + "WindowTop", Top);
            key.SetValue(PluginName + "WindowLeft", Left);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            key.SetValue(PluginName + "WindowHeight", Height);
            key.SetValue(PluginName + "WindowWidth", Width);
            tileBar.CaptionHeight = ActualHeight - 10;
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

                default:
                    return;
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