using DesktopMagic.Plugins;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace DesktopMagic.DataContexts;

internal class PluginEntryDataContext(PluginMetadata pluginMetadata, ICommand command, bool installed = false) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isVisible = true;

    public string Name => pluginMetadata.Name;

    public string? Description => pluginMetadata.Description;

    public string? Logo => pluginMetadata.IconUri?.ToString();

    public DateTime? FormattedDateAdded => pluginMetadata.Added;
    public DateTime? FormattedDateUpdated => pluginMetadata.Updated;

    public uint Id => pluginMetadata.Id;

    public ICommand Command => command;

    public Visibility InstallButtonVisibility => installed ? Visibility.Collapsed : Visibility.Visible;

    public Visibility RemoveButtonVisibility => installed ? Visibility.Visible : Visibility.Collapsed;

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            isVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Visibility));
        }
    }

    public Visibility Visibility => IsVisible ? Visibility.Visible : Visibility.Collapsed;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}