using DesktopMagic.Plugins;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;

namespace DesktopMagic.Settings;

public class PluginSettings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Theme? theme;
    private List<SettingElement> settings = [];
    private bool enabled = false;
    private Point position = new Point(100, 100);
    private Point size = new Point(300, 300);

    [JsonIgnore]
    public Theme Theme => theme is null ? MainWindowDataContext.GetSettings().CurrentLayout.Theme : theme;

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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}