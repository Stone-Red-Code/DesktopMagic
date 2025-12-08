using DesktopMagic.Plugins;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DesktopMagic.Settings;

internal class DesktopMagicSettings : INotifyPropertyChanged
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
            themes.CollectionChanged += (s, e) => CurrentLayout.UpdateTheme();
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
        get => currentLayoutName ?? Layouts.FirstOrDefault()?.Name;
        set
        {
            currentLayoutName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentLayout));
        }
    }

    public string? ModIoAccessToken { get; set; }

    public DesktopMagicSettings()
    {
        themes.CollectionChanged += (s, e) => CurrentLayout.UpdateTheme();

        layouts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CurrentLayout));
        layouts.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CurrentLayoutName));
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}