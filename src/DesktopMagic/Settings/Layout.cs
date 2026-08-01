using DesktopMagic.DataContexts;
using DesktopMagic.Plugins;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DesktopMagic.Settings;

public class Layout(string name) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string name = name;
    private string? currentThemeName = null;
    private Dictionary<uint, PluginSettings> plugins = [];
    private double screenAspectRatio = 0;

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
        get => Theme.Name;
        set
        {
            if (currentThemeName != value)
            {
                currentThemeName = value;
                UpdateTheme();
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

    /// <summary>
    /// Aspect ratio (Width / Height) of the screen this layout was designed for.
    /// Used to match layouts to screens and to make layouts portable across screens.
    /// </summary>
    public double ScreenAspectRatio
    {
        get => screenAspectRatio;
        set
        {
            if (screenAspectRatio != value)
            {
                screenAspectRatio = value;
                OnPropertyChanged();
            }
        }
    }

    public void UpdatePlugins()
    {
        plugins = plugins.ToDictionary();
        OnPropertyChanged(nameof(Plugins));
    }

    public void UpdateTheme()
    {
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(CurrentThemeName));

        foreach (PluginSettings pluginSettings in plugins.Values)
        {
            pluginSettings.UpdateTheme();
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}