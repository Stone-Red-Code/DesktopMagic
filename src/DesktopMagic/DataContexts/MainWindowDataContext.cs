using DesktopMagic.Helpers;
using DesktopMagic.Settings;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopMagic.DataContexts;

internal class MainWindowDataContext : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static DesktopMagicSettings settings = new();
    private bool isLoading = true;

    public DesktopMagicSettings Settings
    {
        get => settings;
        set
        {
            settings = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        set
        {
            isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoStartEnabled
    {
        get => StartupManager.IsAutoStartEnabled();
        set
        {
            if (value)
            {
                _ = StartupManager.EnableAutoStart();
            }
            else
            {
                _ = StartupManager.DisableAutoStart();
            }
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