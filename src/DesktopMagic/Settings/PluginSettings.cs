using DesktopMagic.DataContexts;
using DesktopMagic.Plugins;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;

namespace DesktopMagic.Settings;

public class PluginSettings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string? currentThemeName;
    private List<SettingElement> settings = [];
    private bool enabled = false;
    private Point position = new Point(100, 100);
    private Point size = new Point(300, 300);

    // Only for internal use to show the name of the plugin in the main window
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public Theme Theme
    {
        get
        {
            DesktopMagicSettings settings = MainWindowDataContext.GetSettings();
            return settings.Themes.FirstOrDefault(t => t.Name == currentThemeName) ?? settings.CurrentLayout.Theme;
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

    public List<SettingElement> Settings
    {
        get => settings;
        set
        {
            if (settings != value)
            {
                settings = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Enabled
    {
        get => enabled;
        set
        {
            if (enabled != value)
            {
                enabled = value;
                OnPropertyChanged();
            }
        }
    }

    public Point Position
    {
        get => position;
        set
        {
            if (position != value)
            {
                position = value;
                OnPropertyChanged();
            }
        }
    }

    public Point Size
    {
        get => size;
        set
        {
            if (size != value)
            {
                size = value;
                OnPropertyChanged();
            }
        }
    }

    public void UpdateTheme()
    {
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(CurrentThemeName));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}