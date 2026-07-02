using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DesktopMagic;

public partial class MainWindow : FluentWindow
{
    private readonly Manager _manager = Manager.Instance;
    private readonly MainWindowDataContext _mainWindowDataContext = new();

    [Obsolete]
    public MainWindow()
    {
        SystemThemeWatcher.Watch(this);

        InitializeComponent();

        DataContext = _mainWindowDataContext;

        Resources.MergedDictionaries.Add(App.LanguageDictionary);
        App.DialogService.SetDialogHost(rootContentDialog);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Logger.LogInfo("Loading application", source: "MainWindow");

            _mainWindowDataContext.IsLoading = true;

            // Load plugins and settings through manager
            _manager.LoadSettings();
            _mainWindowDataContext.Settings = _manager.Settings;
            _manager.LoadPlugins();
            _manager.LoadLayout();

            if (_manager.Settings.IsFirstRun)
            {
                _manager.Settings.IsFirstRun = false;
                _ = Activate();
            }
            else
            {
                WindowState = WindowState.Minimized;
                Visibility = Visibility.Collapsed;
                ShowInTaskbar = false;
            }

            _manager.IsLoaded = true;
            _mainWindowDataContext.IsLoading = false;
            await ShowLatestReleaseInfoAfterUpdateAsync();

            App.Logger.LogInfo("Application loaded", source: "MainWindow");
        }
        catch (Exception ex)
        {
            App.Logger.LogError(ex.Message, source: "MainWindow");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = App.AppName,
                Content = ex.ToString(),
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
        }
    }

    private async void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        _ = NavigationView.Navigate(typeof(Pages.MainPage));
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _manager.SaveSettings();

        if (_manager.BlockWindowsClosing)
        {
            e.Cancel = true;
            _manager.SetEditMode(false);
            ShowInTaskbar = false;
            WindowState = WindowState.Minimized;
            Visibility = Visibility.Collapsed;
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Visibility = Visibility.Collapsed;
        UpdateLayout();
        _manager.CloseAllPluginWindows();
        Environment.Exit(0);
    }

    private void NotifyIcon_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Left)
        {
            RestoreWindow();
        }
    }

    internal void RestoreWindow()
    {
        for (int i = 0; i < 10; i++)
        {
            ShowInTaskbar = true;
            Visibility = Visibility.Visible;
            SystemCommands.RestoreWindow(this);
            Topmost = true;
            _ = Activate();
            Topmost = false;
        }
    }

    private void Quit()
    {
        _manager.BlockWindowsClosing = false;
        Close();
    }

    private void ReportBugNavigationViewItem_Click(object sender, RoutedEventArgs e)
    {
        string uri = "https://github.com/Stone-Red-Code/DesktopMagic/issues/new?template=bug_report.md";
        ProcessStartInfo psi = new()
        {
            UseShellExecute = true,
            FileName = uri
        };
        _ = Process.Start(psi);
    }

    private void ReportBugEmailNavigationViewItem_Click(object sender, RoutedEventArgs e)
    {
        string uri = "mailto:contact@me.stone-red.net";
        ProcessStartInfo psi = new()
        {
            UseShellExecute = true,
            FileName = uri
        };

        _ = Process.Start(psi);
    }

    private void RequestFeatureNavigationViewItem_Click(object sender, RoutedEventArgs e)
    {
        string uri = "https://github.com/Stone-Red-Code/DesktopMagic/issues/new?template=feature_request.md";
        ProcessStartInfo psi = new()
        {
            UseShellExecute = true,
            FileName = uri
        };
        _ = Process.Start(psi);
    }

    private void RequestFeatureEmailNavigationViewItem_Click(object sender, RoutedEventArgs e)
    {
        string uri = "mailto:contact@me.stone-red.net";
        ProcessStartInfo psi = new()
        {
            UseShellExecute = true,
            FileName = uri
        };

        _ = Process.Start(psi);
    }

    private void DocumentationNavigationViewItem_Click(object sender, RoutedEventArgs e)
    {
        string uri = "https://github.com/Stone-Red-Code/DesktopMagic";
        ProcessStartInfo psi = new()
        {
            UseShellExecute = true,
            FileName = uri
        };
        _ = Process.Start(psi);
    }

    private void NotifyIcon_LeftClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
        _ = NavigationView.Navigate(typeof(Pages.MainPage));
    }

    private void EditLayoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _manager.SetEditMode(!_manager.IsEditMode);
    }

    private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Quit();
    }

    private async Task ShowLatestReleaseInfoAfterUpdateAsync()
    {
        string currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        string? lastShownVersion = _manager.Settings.ReleaseInfoLastAppVersion;

        if (string.Equals(lastShownVersion, currentVersion, StringComparison.Ordinal))
        {
            return;
        }

        GitHubLatestRelease? latestRelease = await GetLatestReleaseInfoAsync();
        if (latestRelease is null)
        {
            return;
        }

        string releaseName = !string.IsNullOrWhiteSpace(latestRelease.Name) ? latestRelease.Name : latestRelease.TagName;
        string publishedAt = latestRelease.PublishedAt?.ToLocalTime().ToString("g") ?? (string)FindResource("unknown");
        string publishedAtText = string.Format((string)FindResource("releaseInfoPublishedFormat"), publishedAt);
        string releaseNotesMarkdown = string.IsNullOrWhiteSpace(latestRelease.Body) ? "-" : latestRelease.Body.Trim();

        ReleaseInfoDialog releaseInfoDialog = new(
            (string)FindResource("releaseInfoTitle"),
            releaseName,
            publishedAtText,
            releaseNotesMarkdown,
            (string)FindResource("openReleasePage"),
            (string)FindResource("ok"))
        {
            Owner = this,
            Topmost = true
        };

        _ = releaseInfoDialog.ShowDialog();

        if (releaseInfoDialog.OpenReleaseRequested)
        {
            ProcessStartInfo processStartInfo = new()
            {
                UseShellExecute = true,
                FileName = latestRelease.HtmlUrl
            };
            _ = Process.Start(processStartInfo);
        }

        _manager.Settings.ReleaseInfoLastAppVersion = currentVersion;
        _manager.SaveSettings();
    }

    private static async Task<GitHubLatestRelease?> GetLatestReleaseInfoAsync()
    {
        try
        {
            return await GitHubReleaseService.GetLatestReleaseInfoAsync();
        }
        catch (HttpRequestException ex)
        {
            App.Logger.LogError($"Failed to retrieve latest release information: {ex.Message}", source: "MainWindow");
        }
        catch (TaskCanceledException ex)
        {
            App.Logger.LogWarn($"Timed out while retrieving latest release information: {ex.Message}", source: "MainWindow");
        }
        catch (JsonException ex)
        {
            App.Logger.LogError($"Failed to parse latest release information: {ex.Message}", source: "MainWindow");
        }

        return null;
    }
}
