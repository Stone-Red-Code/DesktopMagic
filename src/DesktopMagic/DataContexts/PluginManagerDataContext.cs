using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopMagic.DataContexts;

internal class PluginManagerDataContext : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string allPluginsSearchText = string.Empty;
    private string installedPluginsSearchText = string.Empty;
    private bool isLoading = true;
    private bool isSearching;
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

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}