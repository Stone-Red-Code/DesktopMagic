using DesktopMagic.Settings;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopMagic;

internal class MainWindowDataContext : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static DesktopMagicSettings settings = new();

    public DesktopMagicSettings Settings
    {
        get => settings;
        set
        {
            settings = value;
            OnPropertyChanged();
        }
    }

    public static DesktopMagicSettings GetSettings()
    {
        return settings;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}