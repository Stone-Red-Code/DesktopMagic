using DesktopMagic.Api.Settings;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using Microsoft.Web.WebView2.Core;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopMagic;

public partial class WebPluginWindow : Window, IPluginWindow
{
    public event Action? PluginLoaded;

    public event Action? OnExit;

    private readonly PluginSettings settings;
    private bool isInitialized = false;
    private FileSystemWatcher? fileWatcher;
    private System.Timers.Timer? reloadDebounceTimer;
    private bool isReloading = false;

    private readonly System.Drawing.Rectangle screenBounds;
    private readonly string screenDeviceName;
    private bool isUpdatingPosition = false;

    // Event handlers on long-lived settings objects, tracked so they can be unsubscribed on close.
    private readonly PropertyChangedEventHandler settingsPropertyChangedHandler;
    private readonly PropertyChangedEventHandler themePropertyChangedHandler;
    private Theme? subscribedTheme;
    private readonly List<(Setting Setting, Action Handler)> subscribedSettings = [];
    private readonly List<(Button Button, Action Handler)> subscribedButtonClicks = [];

    public bool IsRunning { get; private set; } = true;
    public PluginMetadata PluginMetadata { get; private set; }
    public string PluginFolderPath { get; private set; }
    public string ScreenDeviceName => screenDeviceName;

    public WebPluginWindow(PluginMetadata pluginMetadata, PluginSettings settings, string pluginFolderPath, System.Drawing.Rectangle screenBounds, string screenDeviceName)
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

        settingsPropertyChangedHandler = (_, s) =>
        {
            if (s.PropertyName == nameof(PluginSettings.CurrentThemeName))
            {
                SubscribeToTheme(settings.Theme);
                ThemeChanged();
            }
            else if (s.PropertyName == nameof(PluginSettings.Position))
            {
                UpdatePosition();
            }
            else if (s.PropertyName == nameof(PluginSettings.Size))
            {
                UpdateSize();
            }
        };
        settings.PropertyChanged += settingsPropertyChangedHandler;

        themePropertyChangedHandler = (_, _) => ThemeChanged();
        SubscribeToTheme(settings.Theme);

        PluginMetadata = pluginMetadata;
        this.settings = settings;
        this.screenBounds = screenBounds;
        this.screenDeviceName = screenDeviceName;

        Point position = ScreenUtilities.PercentToPosition(settings.Position, screenBounds);
        Point size = ScreenUtilities.PercentSizeToSize(settings.Size, screenBounds);
        Left = position.X;
        Top = position.Y;
        Width = size.X;
        Height = size.Y;

        PluginFolderPath = pluginFolderPath;

        if (pluginMetadata.SupportsUnloading && !string.IsNullOrEmpty(pluginFolderPath))
        {
            InitializeHotReload();
        }
    }

    private void SubscribeToTheme(Theme theme)
    {
        if (ReferenceEquals(subscribedTheme, theme))
        {
            return;
        }

        if (subscribedTheme is not null)
        {
            subscribedTheme.PropertyChanged -= themePropertyChangedHandler;
        }

        subscribedTheme = theme;
        subscribedTheme.PropertyChanged += themePropertyChangedHandler;
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
            _ = W32.EnableWindow(webView.Handle, false);
            WindowPos.SetIsLocked(this, false);
            tileBar.CaptionHeight = tileBar.CaptionHeight = ActualHeight - 10 < 0 ? 0 : ActualHeight - 10;
            ResizeMode = ResizeMode.CanResize;
        }
        else
        {
            Topmost = false;
            panel.Visibility = Visibility.Collapsed;
            _ = W32.EnableWindow(webView.Handle, true);
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

        WindowInteropHelper helper = new(this);
        _ = WindowPos.SetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE,
        WindowPos.GetWindowLong(helper.Handle, WindowPos.GWL_EXSTYLE) | WindowPos.WS_EX_NOACTIVATE);
    }

    private async void Window_ContentRendered(object? sender, EventArgs e)
    {
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Starting web plugin", source: "WebPlugin");

        try
        {
            await InitializeWebView();
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - {ex}", source: "WebPlugin");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = $"Error \"{PluginMetadata.Name}\"",
                Content = "WebView2 initialization error:\n" + ex,
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
            Exit();
        }
    }

    private void ThemeChanged()
    {
        webView.Margin = new Thickness(settings.Theme.Margin);
        border.Background = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(settings.Theme.BackgroundColor));
        border.CornerRadius = new CornerRadius(settings.Theme.CornerRadius);

        string cssVariables = $@"
                :root {{
                    --background-color: {MultiColorConverter.ConvertToHexRgba(settings.Theme.BackgroundColor)};
                    --primary-color: {MultiColorConverter.ConvertToHexRgba(settings.Theme.PrimaryColor)};
                    --secondary-color: {MultiColorConverter.ConvertToHexRgba(settings.Theme.SecondaryColor)};
                    --font-family: {settings.Theme.Font};

                    font-family: {settings.Theme.Font};
                    color: {MultiColorConverter.ConvertToHexRgba(settings.Theme.PrimaryColor)};
                }}
            ";

        string script = $@"
                (function() {{
                    let style = document.getElementById('desktop-magic-theme');
                    if (!style) {{
                        style = document.createElement('style');
                        style.id = 'desktop-magic-theme';
                        document.head.appendChild(style);
                    }}
                    style.textContent = `{cssVariables}`;
                }})();
            ";

        _ = webView.ExecuteScriptAsync(script);
    }

    private async System.Threading.Tasks.Task InitializeWebView()
    {
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Initializing WebView2", source: "WebPlugin");

        string htmlPath = Path.Combine(PluginFolderPath, "main.html");

        if (!File.Exists(htmlPath))
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - File \"main.html\" does not exist", source: "WebPlugin");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = $"Error \"{PluginMetadata.Name}\"",
                Content = "File \"main.html\" does not exist!",
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
            Exit();
            return;
        }

        try
        {
            LoadWebPluginSettings();

            string userDataFolder = Path.Combine(Path.GetTempPath(), "DesktopMagic", "WebView2", PluginMetadata.Id.ToString());
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await webView.EnsureCoreWebView2Async(environment);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

            _ = await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GetSettingsBridgeScript());

            string htmlUri = new Uri(htmlPath).AbsoluteUri;
            webView.Source = new Uri(htmlUri);

            isInitialized = true;

            App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - WebView2 initialized successfully", source: "WebPlugin");
            PluginLoaded?.Invoke();

            busyMask.IsBusy = false;
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - WebView2 initialization failed: {ex}", source: "WebPlugin");
            throw;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Stopping web plugin", source: "WebPlugin");
        IsRunning = false;

        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
        }

        reloadDebounceTimer?.Stop();
        reloadDebounceTimer?.Dispose();
        reloadDebounceTimer = null;

        UnsubscribeEvents();

        try
        {
            if (isInitialized && webView.CoreWebView2 != null)
            {
                webView.Dispose();
            }
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - {ex}", source: "WebPlugin");
        }
    }

    private void UnsubscribeEvents()
    {
        settings.PropertyChanged -= settingsPropertyChangedHandler;

        if (subscribedTheme is not null)
        {
            subscribedTheme.PropertyChanged -= themePropertyChangedHandler;
            subscribedTheme = null;
        }

        foreach ((Setting setting, Action handler) in subscribedSettings)
        {
            setting.OnValueChanged -= handler;
        }
        subscribedSettings.Clear();

        foreach ((Button button, Action handler) in subscribedButtonClicks)
        {
            button.OnClick -= handler;
        }
        subscribedButtonClicks.Clear();
    }

    private void UpdatePosition()
    {
        if (isUpdatingPosition)
        {
            return;
        }

        Point position = ScreenUtilities.PercentToPosition(settings.Position, screenBounds);
        if (position == new Point(Left, Top))
        {
            return;
        }

        isUpdatingPosition = true;
        Left = position.X;
        Top = position.Y;
        isUpdatingPosition = false;
    }

    private void UpdateSize()
    {
        if (isUpdatingPosition)
        {
            return;
        }

        Point size = ScreenUtilities.PercentSizeToSize(settings.Size, screenBounds);
        if (size == new Point(Width, Height))
        {
            return;
        }

        isUpdatingPosition = true;
        Width = size.X;
        Height = size.Y;
        isUpdatingPosition = false;
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        if (isUpdatingPosition)
        {
            return;
        }

        Point topLeft = new(Left, Top);
        Point clamped = ScreenUtilities.ClampToScreenBounds(topLeft, new Size(ActualWidth, ActualHeight), screenBounds);

        if (clamped != topLeft)
        {
            isUpdatingPosition = true;
            Left = clamped.X;
            Top = clamped.Y;
            isUpdatingPosition = false;
        }

        settings.Position = ScreenUtilities.PositionToPercent(new Point(Left, Top), screenBounds);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!isUpdatingPosition)
        {
            settings.Size = ScreenUtilities.SizeToPercent(new Point(Width, Height), screenBounds);
        }

        tileBar.CaptionHeight = ActualHeight - 10;
    }

    private bool firstLoad = true;

    private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        webView.CoreWebView2.DOMContentLoaded += (_, _) =>
        {
            if (firstLoad)
            {
                firstLoad = false;
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = true;
                }
            }

            ThemeChanged();
        };
    }

    private void LoadWebPluginSettings()
    {
        string settingsPath = Path.Combine(PluginFolderPath, "settings.json");
        if (!File.Exists(settingsPath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(settingsPath);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                App.Logger.LogWarn($"\"{PluginMetadata.Name}\" - settings.json must be a JSON array", source: "WebPlugin");
                return;
            }

            List<SettingElement> settingElements = [];
            int orderIndex = 0;

            foreach (JsonElement element in root.EnumerateArray())
            {
                string id = element.GetProperty("id").GetString() ?? $"setting-{orderIndex}";
                string name = element.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() ?? id : id;
                string type = element.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() ?? "textbox" : "textbox";

                Setting? setting = CreateSetting(type, element);
                if (setting is null)
                {
                    App.Logger.LogWarn($"\"{PluginMetadata.Name}\" - Unknown setting type \"{type}\" for \"{id}\"", source: "WebPlugin");
                    continue;
                }

                SettingElement settingElement = new(setting, id, name, orderIndex);

                if (settings.Settings.Exists(e => e.Id == id))
                {
                    SettingElement saved = settings.Settings.First(e => e.Id == id);
                    settingElement.JsonValue = saved.JsonValue;
                }

                string capturedId = id;
                if (setting is Button button)
                {
                    Action clickHandler = () =>
                    {
                        _ = webView.Dispatcher.InvokeAsync(async () =>
                        {
                            try
                            {
                                _ = await webView.ExecuteScriptAsync($"window.desktopMagic?.onClick?.({JsonSerializer.Serialize(capturedId)})");
                            }
                            catch (Exception ex)
                            {
                                App.Logger.LogError($"\"{PluginMetadata.Name}\" - Failed to notify button click for \"{capturedId}\": {ex}", source: "WebPlugin");
                            }
                        });
                    };
                    button.OnClick += clickHandler;
                    subscribedButtonClicks.Add((button, clickHandler));
                }

                Action valueChangedHandler = () =>
                {
                    _ = webView.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            await NotifySettingChange(capturedId, setting);
                        }
                        catch (Exception ex)
                        {
                            App.Logger.LogError($"\"{PluginMetadata.Name}\" - Failed to notify setting change for \"{capturedId}\": {ex}", source: "WebPlugin");
                        }
                    });
                };
                setting.OnValueChanged += valueChangedHandler;
                subscribedSettings.Add((setting, valueChangedHandler));

                settingElements.Add(settingElement);
                orderIndex++;
            }

            settings.Settings = [.. settingElements.OrderBy(x => x.OrderIndex)];
        }
        catch (Exception ex)
        {
            App.Logger.LogWarn($"\"{PluginMetadata.Name}\" - Failed to load settings from settings.json: {ex.Message}", source: "WebPlugin");
        }
    }

    private string GetSettingsBridgeScript()
    {
        string settingsJson = SerializeSettingsToJson();

        return $@"
            (function() {{
                window.desktopMagic = {{}};
                window.desktopMagic._settings = {settingsJson};

                window.desktopMagic.getSettings = function() {{
                    return JSON.parse(JSON.stringify(window.desktopMagic._settings));
                }};

                window.desktopMagic.getSetting = function(id) {{
                    return window.desktopMagic._settings ? window.desktopMagic._settings[id] : undefined;
                }};

                window.desktopMagic.onSettingChanged = null;
                window.desktopMagic.onButtonClick = null;

                window.desktopMagic.dispatchSettingChanged = function(id, value) {{
                    if (window.desktopMagic._settings) {{
                        window.desktopMagic._settings[id] = value;
                    }}
                    if (typeof window.desktopMagic.onSettingChanged === 'function') {{
                        window.desktopMagic.onSettingChanged(id, value);
                    }}
                }};

                window.desktopMagic.onClick = function(id) {{
                    if (typeof window.desktopMagic.onButtonClick === 'function') {{
                        window.desktopMagic.onButtonClick(id);
                    }}
                }};
            }})();
        ";
    }

    private void InitializeHotReload()
    {
        try
        {
            fileWatcher = new FileSystemWatcher(PluginFolderPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false
            };

            fileWatcher.Changed += OnPluginFileChanged;

            reloadDebounceTimer = new System.Timers.Timer(500)
            {
                AutoReset = false
            };
            reloadDebounceTimer.Elapsed += async (s, e) =>
            {
                await ReloadWebPlugin();
            };

            App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Hot reload enabled", source: "WebPlugin");
        }
        catch (Exception ex)
        {
            App.Logger.LogWarn($"\"{PluginMetadata.Name}\" - Failed to initialize hot reload: {ex.Message}", source: "WebPlugin");
        }
    }

    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        if (isReloading)
        {
            return;
        }

        string? fileName = e.Name;
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not ".html" and not ".json" and not ".js" and not ".css")
        {
            return;
        }

        reloadDebounceTimer?.Stop();
        reloadDebounceTimer?.Start();
    }

    private async Task ReloadWebPlugin()
    {
        if (isReloading || !IsRunning)
        {
            return;
        }

        isReloading = true;

        try
        {
            App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Reloading web plugin", source: "WebPlugin");

            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
            }

            LoadWebPluginSettings();

            await Dispatcher.Invoke(async () =>
            {
                if (isInitialized && webView.CoreWebView2 != null)
                {
                    _ = await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GetSettingsBridgeScript());

                    busyMask.IsBusy = true;

                    string htmlPath = Path.Combine(PluginFolderPath, "main.html");
                    string htmlUri = new Uri(htmlPath).AbsoluteUri;
                    webView.CoreWebView2.Navigate(htmlUri);

                    busyMask.IsBusy = false;
                }
            });

            App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - Web plugin reloaded", source: "WebPlugin");
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"\"{PluginMetadata.Name}\" - Failed to reload web plugin: {ex}", source: "WebPlugin");
        }
        finally
        {
            isReloading = false;

            if (fileWatcher != null && IsRunning)
            {
                fileWatcher.EnableRaisingEvents = true;
            }
        }
    }

    private async System.Threading.Tasks.Task NotifySettingChange(string id, Setting setting)
    {
        object? value = GetSettingValue(setting);
        string serializedValue = JsonSerializer.Serialize(value);
        string serializedId = JsonSerializer.Serialize(id);
        _ = await webView.ExecuteScriptAsync($"window.desktopMagic?.dispatchSettingChanged?.({serializedId}, {serializedValue})");
    }

    private string SerializeSettingsToJson()
    {
        Dictionary<string, object?> dict = [];
        foreach (SettingElement element in settings.Settings)
        {
            dict[element.Id] = GetSettingValue(element.Input!);
        }
        return JsonSerializer.Serialize(dict);
    }

    private static object? GetSettingValue(Setting setting)
    {
        return setting switch
        {
            TextBox tb => tb.Value,
            CheckBox cb => cb.Value,
            Slider sl => sl.Value,
            IntegerUpDown iud => iud.Value,
            ComboBox cb => cb.Value,
            ColorPicker cp => MultiColorConverter.ConvertToHexRgba(cp.Value),
            Button btn => btn.Value,
            Label lbl => lbl.Value,
            FileSelector fs => fs.Value,
            _ => null
        };
    }

    private static Setting? CreateSetting(string type, JsonElement element)
    {
        return type.ToLowerInvariant() switch
        {
            "textbox" => new TextBox(
                element.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetString() ?? "" : ""),

            "checkbox" => new CheckBox(
                element.TryGetProperty("default", out JsonElement defaultValue) && defaultValue.ValueKind == JsonValueKind.True),

            "slider" => new Slider(
                element.TryGetProperty("min", out JsonElement minValue) ? minValue.GetDouble() : 0,
                element.TryGetProperty("max", out JsonElement maxValue) ? maxValue.GetDouble() : 100,
                element.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetDouble() : 0),

            "integer" => new IntegerUpDown(
                element.TryGetProperty("min", out JsonElement minValue) ? minValue.GetInt32() : 0,
                element.TryGetProperty("max", out JsonElement maxValue) ? maxValue.GetInt32() : 100,
                element.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetInt32() : 0),

            "combobox" => CreateComboBox(element),

            "colorpicker" => new ColorPicker(
                element.TryGetProperty("default", out JsonElement defaultValue)
                    ? MultiColorConverter.ConvertToSystemColor(defaultValue.GetString() ?? "#FFFFFFFF", System.Drawing.Color.White)
                    : System.Drawing.Color.White),

            "button" => new Button(
                element.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetString() ?? "" : ""),

            "label" => new Label(
                element.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetString() ?? "" : "",
                element.TryGetProperty("bold", out JsonElement boldValue) && boldValue.ValueKind == JsonValueKind.True),

            "file" => new FileSelector(
                element.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetString() ?? "" : "",
                element.TryGetProperty("filter", out JsonElement filterValue) ? filterValue.GetString() ?? "All Files|*.*" : "All Files|*.*",
                element.TryGetProperty("title", out JsonElement titleValue) ? titleValue.GetString() ?? "Select File" : "Select File",
                element.TryGetProperty("selectFolder", out JsonElement selectFolderValue) && selectFolderValue.ValueKind == JsonValueKind.True),

            _ => null
        };
    }

    private static ComboBox CreateComboBox(JsonElement element)
    {
        List<string> items = [];
        if (element.TryGetProperty("items", out JsonElement itemsValue) && itemsValue.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in itemsValue.EnumerateArray())
            {
                items.Add(item.GetString() ?? "");
            }
        }

        ComboBox comboBox = new([.. items]);

        if (element.TryGetProperty("default", out JsonElement defaultValue))
        {
            string defaultText = defaultValue.GetString() ?? "";
            if (!string.IsNullOrEmpty(defaultText) && items.Contains(defaultText))
            {
                comboBox.Value = defaultText;
            }
        }

        return comboBox;
    }
}
