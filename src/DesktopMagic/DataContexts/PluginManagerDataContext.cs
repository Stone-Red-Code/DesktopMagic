using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

using Wpf.Ui.Appearance;

namespace DesktopMagic.DataContexts;

internal class PluginManagerDataContext : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string allPluginsSearchText = string.Empty;
    private string installedPluginsSearchText = string.Empty;
    private bool isLoading = true;
    private bool isSearching;
    private bool isAuthenticated = false;
    public ObservableCollection<PluginEntryDataContext> AllPlugins { get; } = [];
    public ObservableCollection<PluginEntryDataContext> InstalledPlugins { get; } = [];

    public bool IsLoading
    {
        get => isLoading;
        set
        {
            isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    public bool IsNotLoading => !IsLoading;

    public bool IsSearching
    {
        get => isSearching;
        set
        {
            isSearching = value;
            OnPropertyChanged();
        }
    }

    public string AllPluginsSearchText
    {
        get => allPluginsSearchText;
        set
        {
            allPluginsSearchText = value;
            OnPropertyChanged();
        }
    }

    public string InstalledPluginsSearchText
    {
        get => installedPluginsSearchText;
        set
        {
            installedPluginsSearchText = value;
            OnPropertyChanged();
        }
    }

    public bool IsAuthenticated
    {
        get => isAuthenticated;
        set
        {
            isAuthenticated = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LoginButtonText));
        }
    }

    public BitmapImage ModIoIcon
    {
        get
        {
            ApplicationTheme theme = ApplicationThemeManager.GetAppTheme();

            if (theme == ApplicationTheme.Light)
            {
                return Application.Current.Resources["ModioLogoBlueDark"] as BitmapImage ?? new BitmapImage();
            }
            else
            {
                return Application.Current.Resources["ModioLogoBlueLight"] as BitmapImage ?? new BitmapImage();
            }
        }
    }

    public string LoginButtonText => isAuthenticated ? (string)App.LanguageDictionary["logout"] : (string)App.LanguageDictionary["login"];

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}