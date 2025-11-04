using DesktopMagic.DataContexts;
using DesktopMagic.Plugins;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DesktopMagic.Settings;

internal class Layout(string name) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string name = name;
    private string? currentThemeName = null;
    private Dictionary<uint, PluginSettings> plugins = [];

    [JsonIgnore]
    public Theme Theme
    {
        get
        {
            DesktopMagicSettings settings = MainWindowDataContext.GetSettings();
            return settings.Themes.FirstOrDefault(theme => theme.Name == currentThemeName) ?? settings.Themes.FirstOrDefault() ?? new Theme("ERROR");
        }
    }

    public string? CurrentThemeName
    {
        get => currentThemeName;
        set
        {
            if (currentThemeName != value)
            {
                currentThemeName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Theme));
            }
        }
    }

    public Dictionary<uint, PluginSettings> Plugins
    {
        get => plugins;
        set
        {
            plugins = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => name;
        set
        {
            name = value;
            OnPropertyChanged();
        }
    }

    public void UpdatePlugins()
    {
        plugins = plugins.ToDictionary();
        OnPropertyChanged(nameof(Plugins));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}