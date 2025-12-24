using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using Microsoft.Web.WebView2.Core;

using System;
using System.IO;
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

    public bool IsRunning { get; private set; } = true;
    public PluginMetadata PluginMetadata { get; private set; }
    public string PluginFolderPath { get; private set; }

    public WebPluginWindow(PluginMetadata pluginMetadata, PluginSettings settings, string pluginFolderPath)
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
            _ = MessageBox.Show("WebView2 initialization error:\n" + ex, $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _ = MessageBox.Show("File \"main.html\" does not exist!", $"Error \"{PluginMetadata.Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
            Exit();
            return;
        }

        try
        {
            string userDataFolder = Path.Combine(Path.GetTempPath(), "DesktopMagic", "WebView2", PluginMetadata.Id.ToString());
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await webView.EnsureCoreWebView2Async(environment);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

            string htmlUri = new Uri(htmlPath).AbsoluteUri;
            webView.Source = new Uri(htmlUri);

            isInitialized = true;

            App.Logger.LogInfo($"\"{PluginMetadata.Name}\" - WebView2 initialized successfully", source: "WebPlugin");
            PluginLoaded?.Invoke();
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

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        settings.Position = new System.Windows.Point(Left, Top);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        settings.Size = new System.Windows.Point(Width, Height);

        tileBar.CaptionHeight = ActualHeight - 10;
    }

    private void webView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        webView.CoreWebView2.DOMContentLoaded += (_, _) => ThemeChanged();
    }
}
