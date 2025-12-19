using DesktopMagic.Api;
using DesktopMagic.Api.Drawing;
using DesktopMagic.Api.Settings;
using DesktopMagic.DataContexts;
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
using System.Runtime.Loader;
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
    private Thread? pluginThread;
    private System.Timers.Timer? updateTimer;
    private Plugin? pluginClassInstance;
    private readonly AssemblyLoadContext assemblyLoadContext;

    public bool IsRunning { get; private set; } = true;
    public PluginMetadata PluginMetadata { get; private set; }
    public string PluginFolderPath { get; private set; }

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

        WindowInteropHelper helper = new WindowInteropHelper(w);
        _ = helper.EnsureHandle();

        Owner = w;

        settings.PropertyChanged += (e, s) =>
        {
            if (s.PropertyName == nameof(PluginSettings.CurrentThemeName))
            {
                settings.Theme.PropertyChanged += (se, ev) =>
                {
                    ThemeChanged();
                };
                ThemeChanged();
            }
        };

        PluginMetadata = pluginMetadata;
        this.settings = settings;

        Left = settings.Position.X;
        Top = settings.Position.Y;
        Width = settings.Size.X;
        Height = settings.Size.Y;

        PluginFolderPath = pluginFolderPath;

        assemblyLoadContext = new AssemblyLoadContext(pluginMetadata.Name, isCollectible: true);
        assemblyLoadContext.Resolving += (context, assemblyName) =>
        {
            string assemblyPath = Path.Combine(PluginFolderPath, assemblyName.Name + ".dll");
            if (File.Exists(assemblyPath))
            {
                return context.LoadFromAssemblyPath(assemblyPath);
            }
            else
            {
                _ = assemblyLoadContext.LoadFromAssemblyName(assemblyName);
            }
            return null;
        };
    }

    public PluginWindow(Plugin pluginClassInstance, PluginMetadata pluginMetadata, PluginSettings settings) : this(pluginMetadata, settings, string.Empty)
    {
        this.pluginClassInstance = pluginClassInstance;
    }

    public void UpdatePluginWindow()
    {
        Dispatcher.Invoke(ThemeChanged);
        UpdateTimer_Elapsed(updateTimer, null);
    }

    public void Exit()
    {
        IsRunning = false;

        Dispatcher.Invoke(() =>
        {
            OnExit?.Invoke();
        });
    }

    public void SetEditMode(bool enabled)
    {
        if (enabled)
        {
            Topmost = true;
            panel.Visibility = Visibility.Visible;
            imageBorder.BorderThickness = new Thickness(3);
            image.Margin = new(-3);
            WindowPos.SetIsLocked(this, false);
            tileBar.CaptionHeight = tileBar.CaptionHeight = ActualHeight - 10 < 0 ? 0 : ActualHeight - 10;
            ResizeMode = ResizeMode.CanResize;
        }
        else
        {
            Topmost = false;
            panel.Visibility = Visibility.Collapsed;
            imageBorder.BorderThickness = new Thickness(0);
            image.Margin = new Thickness(0);
            WindowPos.SendWpfWindowBack(this);
            WindowPos.SendWpfWindowBack(this);
            WindowPos.SetIsLocked(this, true);
            tileBar.CaptionHeight = 0;
            ResizeMode = ResizeMode.NoResize;
        }
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
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Starting plugin thread", source: "Plugin");
        pluginThread = new Thread(LoadPlugin);
        pluginThread.Start();
    }

    private void ThemeChanged()
    {
        viewBox.Margin = new Thickness(settings.Theme.Margin);
        border.Background = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(settings.Theme.BackgroundColor));
        border.CornerRadius = new CornerRadius(settings.Theme.CornerRadius);
    }

    private void LoadPlugin()
    {
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Loading plugin", source: "Plugin");

        if (pluginClassInstance is null && !File.Exists($"{PluginFolderPath}\\main.dll"))
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - File \"main.dll\" does not exist", source: "Plugin");
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
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - {ex}", source: "Plugin");
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
            byte[] assemblyData = File.ReadAllBytes($"{PluginFolderPath}\\main.dll");
            using MemoryStream assemblyStream = new(assemblyData);

            Assembly dll = assemblyLoadContext.LoadFromStream(assemblyStream);

            Type? instanceType = Array.Find(dll.GetTypes(), type => type.GetTypeInfo().BaseType == typeof(Plugin));

            if (instanceType is null)
            {
                App.Logger.LogError($"\"{PluginMetadata.Name}\" - The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", source: "Plugin");
                _ = MessageBox.Show($"The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);

                Exit();
                return;
            }

            instance = Activator.CreateInstance(instanceType);
        }
        if (instance is Plugin plugin)
        {
            pluginClassInstance = plugin;
            pluginClassInstance.Application = new PluginData(this, settings);
        }
        else
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - The \"Plugin\" class could not be found! It has to inherit from \"{typeof(Plugin).FullName}\"", source: "Plugin");
            _ = MessageBox.Show($"The \"Plugin\" class has to inherit from \"{typeof(Plugin).FullName}\"", $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
            return;
        }

        LoadOptions(pluginClassInstance);
        BindDefaultSettings(pluginClassInstance);

        updateTimer = new System.Timers.Timer
        {
            Interval = 1000
        };
        updateTimer.Elapsed += UpdateTimer_Elapsed;

        pluginClassInstance.Start();
        UpdatePluginWindow();

        if (pluginClassInstance.UpdateInterval > 0)
        {
            updateTimer.Interval = pluginClassInstance.UpdateInterval;
            updateTimer.Start();
        }
    }

    private void BindDefaultSettings(Plugin pluginClassInstance)
    {
        Dispatcher.Invoke(() =>
        {
            SetHorizontalAlignment();
            SetVerticalAlignment();
            SetThemeOverride();
            SetThemeOverrideItems();

            pluginClassInstance.horizontalAlignment.OnValueChanged += SetHorizontalAlignment;
            pluginClassInstance.verticalAlignment.OnValueChanged += SetVerticalAlignment;
            pluginClassInstance.themeOverride.OnValueChanged += SetThemeOverride;

            DesktopMagicSettings desktopMagicSettings = MainWindowDataContext.GetSettings();
            desktopMagicSettings.Themes.CollectionChanged += (s, e) => SetThemeOverrideItems();
        });

        void SetVerticalAlignment()
        {
            VerticalAlignment verticalAlignment = pluginClassInstance.verticalAlignment.Value switch
            {
                "Top" => VerticalAlignment.Top,
                "Center" => VerticalAlignment.Center,
                "Bottom" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };

            viewBox.VerticalAlignment = verticalAlignment;
            border.VerticalAlignment = verticalAlignment;
        }

        void SetHorizontalAlignment()
        {
            HorizontalAlignment horizontalAlignment = pluginClassInstance.horizontalAlignment.Value switch
            {
                "Left" => HorizontalAlignment.Left,
                "Center" => HorizontalAlignment.Center,
                "Right" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center
            };

            viewBox.HorizontalAlignment = horizontalAlignment;
            border.HorizontalAlignment = horizontalAlignment;
        }

        void SetThemeOverride()
        {
            if (pluginClassInstance.themeOverride.Value == "<None>")
            {
                settings.CurrentThemeName = null;
            }
            else
            {
                settings.CurrentThemeName = pluginClassInstance.themeOverride.Value;
            }
        }

        void SetThemeOverrideItems()
        {
            DesktopMagicSettings desktopMagicSettings = MainWindowDataContext.GetSettings();

            pluginClassInstance.themeOverride.Items.Clear();
            pluginClassInstance.themeOverride.Items.Add("<None>");

            foreach (Theme theme in desktopMagicSettings.Themes)
            {
                pluginClassInstance.themeOverride.Items.Add(theme.Name);
            }
        }
    }

    private void LoadOptions(object instance)
    {
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Loading plugin options", source: "Plugin");

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
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - {ex}", source: "Plugin");
            _ = MessageBox.Show("File execution error:\n" + ex, $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
        }
    }

    private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs? e)
    {
        try
        {
            if (IsRunning && pluginClassInstance is not null)
            {
                Bitmap? result = pluginClassInstance.Main();

                if (pluginClassInstance.UpdateInterval > 0)
                {
                    updateTimer!.Interval = pluginClassInstance.UpdateInterval;
                }
                else
                {
                    updateTimer!.Stop();
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
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - {ex}", source: "Plugin");
            _ = MessageBox.Show("File execution error:\n" + ex, $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
            return;
        }

        if (!IsRunning)
        {
            updateTimer!.Stop();
        }
    }

    private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        rectangleGeometry.Rect = new Rect(-settings.Theme.Margin, -settings.Theme.Margin, e.NewSize.Width, e.NewSize.Height);
    }

    private void ViewBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        border.Width = e.NewSize.Width + (settings.Theme.Margin * 2);
        border.Height = e.NewSize.Height + (settings.Theme.Margin * 2);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Stopping plugin", source: "Plugin");
        IsRunning = false;

        try
        {
            pluginClassInstance?.Stop();
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - {ex}", source: "Plugin");
        }

        assemblyLoadContext.Unload();
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