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

internal class PluginEntryDataContext(PluginMetadata pluginMetadata, ICommand command, PluginEntryDataContext.Mode mode, string? path = null) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isVisible = true;

    public string Name => pluginMetadata.Name;

    public string? Description => pluginMetadata.Description;

    public string Author => pluginMetadata.Author ?? (string)App.GetLanguageDictionary()["unknown"];

    public string? Version => pluginMetadata.Version ?? (string)App.GetLanguageDictionary()["unknown"];

    public string? Logo => pluginMetadata.IconUri?.ToString();

    public DateTime? DateAdded => pluginMetadata.Added;
    public DateTime? DateUpdated => pluginMetadata.Updated;

    public uint Id => pluginMetadata.Id;

    public ICommand Command => command;

    public ButtonData InstallUninstallButtonData => new(mode == Mode.Install ? PackIconKind.Download : PackIconKind.Remove, GetInstallUninstallButtonText(), true, Command);

    public ButtonData OpenButtonData
    {
        get
        {
            if (string.IsNullOrWhiteSpace(pluginMetadata.ProfileUri?.ToString()))
            {
                return new(PackIconKind.FolderOutline, (string)App.GetLanguageDictionary()["folder"], path is not null, new CommandHandler(() => Process.Start("explorer.exe", path!)));
            }
            else
            {
                return new(PackIconKind.ExternalLink, "mod.io", true, new CommandHandler(OpenModioPage));
            }
        }
    }

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

    private void OpenModioPage()
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = pluginMetadata.ProfileUri?.ToString() ?? path
        };

        _ = Process.Start(psi);
    }

    private string GetInstallUninstallButtonText()
    {
        return mode switch
        {
            Mode.Install => (string)App.GetLanguageDictionary()["install"],
            Mode.Uninstall => (string)App.GetLanguageDictionary()["uninstall"],
            _ => throw new NotImplementedException()
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record ButtonData(PackIconKind IconKind, string Text, bool IsEnabled, ICommand Command);

    public enum Mode
    {
        Install,
        Uninstall
    }
}