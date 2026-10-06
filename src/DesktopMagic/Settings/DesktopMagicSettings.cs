using DesktopMagic.Plugins;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DesktopMagic.Settings;

public class DesktopMagicSettings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<Layout> layouts = [];
    private ObservableCollection<Theme> themes = [];
    private string? currentLayoutName;

    public ObservableCollection<Theme> Themes
    {
        get => themes;
        init
        {
            themes = value;
            themes.CollectionChanged += (s, e) =>
            {
                foreach (Layout layout in layouts)
                {
                    layout.UpdateTheme();
                }
            };
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Layout> Layouts
    {
        get => layouts;
        set
        {
            layouts = value;
            layouts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CurrentLayout));
            layouts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CurrentLayoutName));
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Layout CurrentLayout => Layouts.FirstOrDefault(layout => layout.Name == CurrentLayoutName, Layouts.FirstOrDefault() ?? new Layout("ERROR"));

    public string? CurrentLayoutName
    {
        get
        {
            if (!Layouts.Any(l => l.Name == currentLayoutName))
            {
                currentLayoutName = null;
            }

            return currentLayoutName ?? Layouts.FirstOrDefault()?.Name;
        }

        set
        {
            currentLayoutName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentLayout));
        }
    }

    /// <summary>
    /// Maps screen device names (e.g. "\\.\DISPLAY1") to the layout applied on that screen.
    /// The layouts themselves are portable and store their target aspect ratio.
    /// </summary>
    public Dictionary<string, string> ScreenLayouts { get; set; } = [];

    /// <summary>
    /// Version of the settings schema, used to migrate older settings files.
    /// 0 = legacy (layouts not screen aware, pixel based positions).
    /// </summary>
    public int SchemaVersion { get; set; }

    public string? ModIoAccessToken { get; set; }

    public string? ReleaseInfoLastAppVersion { get; set; }

    public bool IsFirstRun { get; set; } = true;

    public DesktopMagicSettings()
    {
        themes.CollectionChanged += (s, e) =>
        {
            foreach (Layout layout in layouts)
            {
                layout.UpdateTheme();
            }
        };

        layouts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CurrentLayout));
        layouts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CurrentLayoutName));
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
