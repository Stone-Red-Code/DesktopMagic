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

    public ScreenDisplay? SelectedScreen
    {
        get => Screens.FirstOrDefault(screen => screen.DeviceName == SelectedScreenId);
        set
        {
            if (value is not null)
            {
                SelectedScreenId = value.DeviceName;
            }
            OnPropertyChanged();
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
        Dictionary<string, System.Drawing.Rectangle> physicalBounds = ScreenUtilities.GetDisplayPhysicalBounds();

        // Capture the hardware id of the currently selected screen so the selection can be
        // preserved across display changes (e.g. unplug/replug renumbers device names).
        ScreenDisplay? previousSelected = SelectedScreen;

        Screens.Clear();
        for (int i = 0; i < allScreens.Count; i++)
        {
            System.Windows.Forms.Screen screen = allScreens[i];
            physicalBounds.TryGetValue(screen.DeviceName, out System.Drawing.Rectangle physicalBoundsRect);
            Screens.Add(new ScreenDisplay(screen, i, physicalBoundsRect, ScreenUtilities.GetMonitorHardwareId(screen)));
        }

        if (selectedScreenDeviceName is null || !Screens.Any(screen => screen.DeviceName == selectedScreenDeviceName))
        {
            ScreenDisplay? byHardwareId = previousSelected is null
                ? null
                : Screens.FirstOrDefault(screen => screen.HardwareId == previousSelected.HardwareId);
            SelectedScreenId = byHardwareId?.DeviceName ?? Screens.FirstOrDefault()?.DeviceName;
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
        OnPropertyChanged(nameof(SelectedScreen));
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
public class ScreenDisplay : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isSelected;
    private double x;
    private double y;
    private double width;
    private double height;

    public ScreenDisplay(System.Windows.Forms.Screen screen, int index, System.Drawing.Rectangle physicalBounds, string hardwareId)
    {
        DeviceName = screen.DeviceName;
        DisplayName = ScreenUtilities.GetScreenLabel(screen, index);
        Bounds = physicalBounds.Width > 0 && physicalBounds.Height > 0 ? physicalBounds : screen.Bounds;
        IsPrimary = screen.Primary;
        Index = index + 1;
        HardwareId = hardwareId;
    }

    public string DeviceName { get; }

    /// <summary>
    /// Stable hardware identifier of the monitor (see <see cref="ScreenUtilities.GetMonitorHardwareId"/>),
    /// used to persist screen bindings across display changes.
    /// </summary>
    public string HardwareId { get; }

    public string DisplayName { get; }

    public System.Drawing.Rectangle Bounds { get; }

    public bool IsPrimary { get; }

    public int Index { get; }

    public string ToolTipText => IsPrimary ? $"{DisplayName} · Primary" : DisplayName;

    /// <summary>
    /// Whether this screen is currently selected in the screen selector.
    /// </summary>
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            OnPropertyChanged();
        }
    }

    // Normalized (control-local) position/size set by the screen selector.
    public double X
    {
        get => x;
        set
        {
            x = value;
            OnPropertyChanged();
        }
    }

    public double Y
    {
        get => y;
        set
        {
            y = value;
            OnPropertyChanged();
        }
    }

    public double Width
    {
        get => width;
        set
        {
            width = value;
            OnPropertyChanged();
        }
    }

    public double Height
    {
        get => height;
        set
        {
            height = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}