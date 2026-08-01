using DesktopMagic.Helpers;
using DesktopMagic.Settings;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DesktopMagic.DataContexts;

internal class MainWindowDataContext : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static DesktopMagicSettings settings = new();
    private static string? selectedScreenDeviceName;
    private string? selectedLayoutName;

    private bool isLoading = true;
    private string? pluginsSearchText;

    public string Title =>
#if DEBUG
        $"{App.AppName} - Dev {System.Windows.Forms.Application.ProductVersion}";
#else
        $"{App.AppName} - {System.Windows.Forms.Application.ProductVersion}";
#endif

    public string AppName => App.AppName;

    public DesktopMagicSettings Settings
    {
        get => settings;
        set
        {
            settings = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ScreenDisplay> Screens { get; } = [];

    public string? SelectedScreenId
    {
        get => selectedScreenDeviceName;
        set
        {
            selectedScreenDeviceName = value;
            UpdateSelection();
        }
    }

    public Layout SelectedLayout => Manager.Instance.SelectedLayout;

    public string? SelectedLayoutName
    {
        get => selectedLayoutName;
        set
        {
            selectedLayoutName = value;
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

    public string? PluginsSearchText
    {
        get => pluginsSearchText;
        set
        {
            pluginsSearchText = value;
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

    /// <summary>
    /// Refreshes the list of detected screens and ensures a screen is selected.
    /// </summary>
    public void RefreshScreens()
    {
        List<System.Windows.Forms.Screen> allScreens = ScreenUtilities.GetAllScreens();

        Screens.Clear();
        for (int i = 0; i < allScreens.Count; i++)
        {
            Screens.Add(new ScreenDisplay(allScreens[i], i));
        }

        if (selectedScreenDeviceName is null || !Screens.Any(screen => screen.DeviceName == selectedScreenDeviceName))
        {
            SelectedScreenId = Screens.FirstOrDefault()?.DeviceName;
        }
        else
        {
            UpdateSelection();
        }
    }

    /// <summary>
    /// Raises change notifications for the currently selected screen's layout.
    /// </summary>
    public void RefreshSelection()
    {
        UpdateSelection();
    }

    /// <summary>
    /// Keeps the manager's screen selection in sync and raises notifications for the selected layout.
    /// </summary>
    private void UpdateSelection()
    {
        Manager.Instance.SelectedScreenDeviceName = selectedScreenDeviceName;
        selectedLayoutName = Manager.Instance.SelectedLayout.Name;
        OnPropertyChanged(nameof(SelectedScreenId));
        OnPropertyChanged(nameof(SelectedLayout));
        OnPropertyChanged(nameof(SelectedLayoutName));
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// A detected screen displayed in the UI.
/// </summary>
public class ScreenDisplay
{
    public ScreenDisplay(System.Windows.Forms.Screen screen, int index)
    {
        DeviceName = screen.DeviceName;
        DisplayName = ScreenUtilities.GetScreenLabel(screen, index);
    }

    public string DeviceName { get; }

    public string DisplayName { get; }
}