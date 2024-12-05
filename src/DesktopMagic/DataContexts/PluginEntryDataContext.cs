using DesktopMagic.Helpers;
using DesktopMagic.Plugins;

using MaterialDesignThemes.Wpf;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace DesktopMagic.DataContexts;

internal class PluginEntryDataContext(PluginMetadata pluginMetadata, ICommand command, string? buttonText = "") : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isVisible = true;

    public string Name => pluginMetadata.Name;

    public string? Description => pluginMetadata.Description;

    public string Author => pluginMetadata.Author ?? "Unknown";

    public string? Version => pluginMetadata.Version ?? "Unknown";

    public string? Logo => pluginMetadata.IconUri?.ToString();

    public DateTime? DateAdded => pluginMetadata.Added;
    public DateTime? DateUpdated => pluginMetadata.Updated;

    public uint Id => pluginMetadata.Id;

    public string? ButtonText => buttonText;

    public ICommand Command => command;

    public ButtonData InstallUninstallButtonData => new(ButtonText ?? "Install", true, Command);
    public ButtonData OpenModioButtonData => new("modio", true, new CommandHandler(OpenModioPage));


    public Visibility ButtonVisibility => string.IsNullOrWhiteSpace(buttonText) ? Visibility.Collapsed : Visibility.Visible;

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
    public MaterialDesignThemes.Wpf.PackIconKind IconKind => MaterialDesignThemes.Wpf.PackIconKind.Information;

    public Visibility Visibility => IsVisible ? Visibility.Visible : Visibility.Collapsed;

    private void OpenModioPage()
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = pluginMetadata.ProfileUri?.ToString()
        };

        _ = Process.Start(psi);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record ButtonData(PackIconKind IconKind, string Text, bool IsEnabled, ICommand Command);
}