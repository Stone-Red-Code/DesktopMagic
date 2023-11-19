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
    private string? currentLayoutName;

    public ObservableCollection<Layout> Layouts
    {
        get => layouts;
        set
        {
            layouts = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Layout CurrentLayout => Layouts.FirstOrDefault(layout => layout.Name == CurrentLayoutName, Layouts.FirstOrDefault() ?? new Layout("ERROR"));

    public string? CurrentLayoutName
    {
        get => currentLayoutName;
        set
        {
            currentLayoutName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentLayout));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}