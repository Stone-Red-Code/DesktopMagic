using DesktopMagic.Api;
using DesktopMagic.Api.Drawing;
using DesktopMagic.Api.Settings;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

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

namespace DesktopMagic;

public partial class PluginWindow : Window
{
    public event Action? PluginLoaded;

    public event Action? OnExit;

    private readonly PluginSettings settings;
    private readonly System.Timers.Timer? updateTimer;
    private Thread? pluginThread;
    private System.Timers.Timer? valueTimer;
    private Plugin? pluginClassInstance;

    public bool IsRunning { get; private set; } = true;
    public PluginMetadata PluginMetadata { get; private set; }
    public string? PluginFolderPath { get; private set; }

    public PluginWindow(PluginMetadata pluginMetadata, PluginSettings settings, string pluginFolderPath)
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

        updateTimer = new System.Timers.Timer
        {
            Interval = 100
        };
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.Start();

        PluginMetadata = pluginMetadata;
        this.settings = settings;

        Left = settings.Position.X;
        Top = settings.Position.Y;
        Width = settings.Size.X;
        Height = settings.Size.Y;

        PluginFolderPath = pluginFolderPath;
    }

    public PluginWindow(Plugin pluginClassInstance, PluginMetadata pluginMetadata, PluginSettings settings) : this(pluginMetadata, settings, string.Empty)
    {
        this.pluginClassInstance = pluginClassInstance;
    }

    public void UpdatePluginWindow()
    {
        ValueTimer_Elapsed(valueTimer, null);
    }

    public void Exit()
    {
        IsRunning = false;

        Dispatcher.Invoke(() =>
        {
            OnExit?.Invoke();
        });
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        //Set the window style to noactivate.
        WindowInteropHelper helper = new(this);
        _ = WindowPos.SetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE,
        WindowPos.GetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE) | WindowPos.WS_EX_NOACTIVATE);
    }

    private static BitmapSource BitmapToImageSource(Bitmap bitmap)
    {
        BitmapData bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, bitmap.PixelFormat);

        BitmapSource bitmapSource = BitmapSource.Create(
            bitmapData.Width, bitmapData.Height,
            bitmap.HorizontalResolution, bitmap.VerticalResolution,
            PixelFormats.Bgra32, null,
            bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

        bitmap.UnlockBits(bitmapData);
        return bitmapSource;
    }

    private void Window_ContentRendered(object? sender, EventArgs e)
    {
        App.Logger.Log($"\"{PluginMetadata.Name}\" - Starting plugin thread", "Plugin");
        pluginThread = new Thread(LoadPlugin);
        pluginThread.Start();
    }

    private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (MainWindow.EditMode)
            {
                panel.Visibility = Visibility.Visible;
                imageBorder.BorderThickness = new Thickness(3);
                image.Margin = new(-3);
                WindowPos.SetIsLocked(this, false);
                tileBar.CaptionHeight = tileBar.CaptionHeight = ActualHeight - 10 < 0 ? 0 : ActualHeight - 10;
                ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                panel.Visibility = Visibility.Collapsed;
                imageBorder.BorderThickness = new Thickness(0);
                image.Margin = new Thickness(0);
                WindowPos.SetIsLocked(this, true);
                tileBar.CaptionHeight = 0;
                ResizeMode = ResizeMode.NoResize;
            }

            if (!IsRunning)
            {
                (sender as System.Timers.Timer)?.Stop();
            }
            else
            {
                viewBox.Margin = new Thickness(settings.Theme.Margin);
                border.Width = viewBox.ActualWidth + (settings.Theme.Margin * 2);
                border.Height = viewBox.ActualHeight + (settings.Theme.Margin * 2);
                rectangleGeometry.Rect = new Rect(-settings.Theme.Margin, -settings.Theme.Margin, border.ActualWidth, border.ActualHeight);
                border.Background = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(settings.Theme.BackgroundColor));
                border.CornerRadius = new CornerRadius(settings.Theme.CornerRadius);
            }
        });

        if (!IsRunning)
        {
            updateTimer?.Stop();
        }
    }

    private void LoadPlugin()
    {
        App.Logger.Log($"\"{PluginMetadata.Name}\" - Loading plugin", "Plugin");

        if (pluginClassInstance is null && !File.Exists($"{PluginFolderPath}\\main.dll"))
        {
            App.Logger.Log($"\"{PluginMetadata.Name}\" - File \"main.dll\" does not exist", "Plugin", LogSeverity.Error);
            _ = MessageBox.Show("File \"main.dll\" does not exist!", $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);

            Exit();
            return;
        }

        try
        {
            ExecuteSource();
        }
        catch (Exception ex)
        {
            App.Logger.Log($"\"{PluginMetadata.Name}\" - {ex}", "Plugin", LogSeverity.Error);
            _ = MessageBox.Show("File execution error:\n" + ex, $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
            return;
        }
        PluginLoaded?.Invoke();
    }

    private void ExecuteSource()
    {
        object? instance = pluginClassInstance;
        if (instance is null)
        {
            byte[] assemblyBytes = File.ReadAllBytes($"{PluginFolderPath}\\main.dll");
            Assembly dll = Assembly.Load(assemblyBytes);
            Type? instanceType = Array.Find(dll.GetTypes(), type => type.GetTypeInfo().BaseType == typeof(Plugin));

            if (instanceType is null)
            {
                App.Logger.Log($"\"{PluginMetadata.Name}\" - The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", "Plugin", LogSeverity.Error);
                _ = MessageBox.Show($"The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);

                Exit();
                return;
            }

            instance = Activator.CreateInstance(instanceType);
        }
        if (instance is Plugin plugin)
        {
            pluginClassInstance = plugin;
            pluginClassInstance.Application = new Plugins.PluginData(this, settings);
        }
        else
        {
            App.Logger.Log($"\"{PluginMetadata.Name}\" - The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", "Plugin", LogSeverity.Error);
            _ = MessageBox.Show($"The \"Plugin\" class has to inherit from \"{typeof(Plugin).FullName}\"", $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
            return;
        }

        LoadOptions(pluginClassInstance);
        BindDefaultSettings(pluginClassInstance);

        valueTimer = new System.Timers.Timer
        {
            Interval = 1000
        };
        valueTimer.Elapsed += ValueTimer_Elapsed;

        pluginClassInstance.Start();
        UpdatePluginWindow();

        if (pluginClassInstance.UpdateInterval > 0)
        {
            valueTimer.Interval = pluginClassInstance.UpdateInterval;
            valueTimer.Start();
        }
    }

    private void BindDefaultSettings(Plugin pluginClassInstance)
    {
        Dispatcher.Invoke(() =>
        {
            SetHorizontalAlignment();
            SetVerticalAlignment();

            pluginClassInstance.horizontalAlignment.OnValueChanged += SetHorizontalAlignment;
            pluginClassInstance.verticalAlignment.OnValueChanged += SetVerticalAlignment;
        });

        void SetVerticalAlignment()
        {
            viewBox.VerticalAlignment = pluginClassInstance.verticalAlignment.Value switch
            {
                "Top" => VerticalAlignment.Top,
                "Center" => VerticalAlignment.Center,
                "Bottom" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };
        }

        void SetHorizontalAlignment()
        {
            viewBox.HorizontalAlignment = pluginClassInstance.horizontalAlignment.Value switch
            {
                "Left" => HorizontalAlignment.Left,
                "Center" => HorizontalAlignment.Center,
                "Right" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center
            };
        }
    }

    private void LoadOptions(object instance)
    {
        App.Logger.Log($"\"{PluginMetadata.Name}\" - Loading plugin options", "Plugin");

        try
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            FieldInfo[] props = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            List<SettingElement> settingElements = [];
            foreach (FieldInfo prop in props)
            {
                if (prop.GetValue(instance) is Setting element)
                {
                    object[] attributes = prop.GetCustomAttributes(true);
                    foreach (object attribute in attributes)
                    {
                        if (attribute is SettingAttribute elementAttribute)
                        {
                            SettingElement settingElement = new SettingElement(element, elementAttribute.Id, elementAttribute.Name, elementAttribute.OrderIndex);

                            if (settings.Settings.Exists(e => e.Id == elementAttribute.Id))
                            {
                                SettingElement settingsSettingElement = settings.Settings.First(e => e.Id == elementAttribute.Id);
                                settingElement.JsonValue = settingsSettingElement.JsonValue;
                            }

                            settingElements.Add(settingElement);
                            break;
                        }
                    }
                }
            }

            settings.Settings = [.. settingElements.OrderBy(x => x.OrderIndex)];
        }
        catch (Exception ex)
        {
            IsRunning = false;
            App.Logger.Log($"\"{PluginMetadata.Name}\" - {ex}", "Plugin", LogSeverity.Error);
            _ = MessageBox.Show("File execution error:\n" + ex, $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
        }
    }

    private void ValueTimer_Elapsed(object? sender, ElapsedEventArgs? e)
    {
        try
        {
            if (IsRunning && pluginClassInstance is not null)
            {
                Bitmap? result = pluginClassInstance.Main();

                if (pluginClassInstance.UpdateInterval > 0)
                {
                    valueTimer!.Interval = pluginClassInstance.UpdateInterval;
                }
                else
                {
                    valueTimer!.Stop();
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
            IsRunning = false;
            App.Logger.Log($"\"{PluginMetadata.Name}\" - {ex}", "Plugin", LogSeverity.Error);
            _ = MessageBox.Show("File execution error:\n" + ex, $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
            return;
        }

        if (!IsRunning)
        {
            valueTimer!.Stop();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        App.Logger.Log($"\"{PluginMetadata.Name}\" - Stopping plugin", "Plugin");
        IsRunning = false;
        pluginClassInstance?.Stop();
    }

    #region Window Events

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        settings.Position = new System.Windows.Point(Left, Top);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        settings.Size = new System.Windows.Point(Width, Height);

        tileBar.CaptionHeight = ActualHeight - 10;
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
        }

        System.Drawing.Point point = new System.Drawing.Point((int)pixelMousePositionX, (int)pixelMousePositionY);
        pluginClassInstance?.OnMouseClick(point, mouseButton);
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ImageSource imageSource = image.Source;
        BitmapSource bitmapImage = (BitmapSource)imageSource;
        double pixelMousePositionX = e.GetPosition(image).X * bitmapImage.PixelWidth / image.ActualHeight;
        double pixelMousePositionY = e.GetPosition(image).Y * bitmapImage.PixelHeight / image.ActualHeight;

        System.Drawing.Point point = new System.Drawing.Point((int)pixelMousePositionX, (int)pixelMousePositionY);
        pluginClassInstance?.OnMouseMove(point);
    }

    private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        ImageSource imageSource = image.Source;
        BitmapSource bitmapImage = (BitmapSource)imageSource;
        double pixelMousePositionX = e.GetPosition(image).X * bitmapImage.PixelWidth / image.ActualHeight;
        double pixelMousePositionY = e.GetPosition(image).Y * bitmapImage.PixelHeight / image.ActualHeight;

        System.Drawing.Point point = new System.Drawing.Point((int)pixelMousePositionX, (int)pixelMousePositionY);
        pluginClassInstance?.OnMouseWheel(point, e.Delta);
    }

    #endregion Window Events
}