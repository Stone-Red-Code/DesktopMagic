using DesktopMagic.Helpers;
using DesktopMagic.Plugins;

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

    public string Author => pluginMetadata.Author ?? (string)App.LanguageDictionary["unknown"];

    public string? Version => pluginMetadata.Version ?? (string)App.LanguageDictionary["unknown"];

    public string? Logo => pluginMetadata.IconUri?.ToString();

    public DateTime? DateAdded => pluginMetadata.Added;
    public DateTime? DateUpdated => pluginMetadata.Updated;

    public uint Id => pluginMetadata.Id;

    public string? Path => path;

    public bool IsLocalPlugin => pluginMetadata.IsLocalPlugin;

    public ICommand Command => command;

    public ButtonData InstallUninstallButtonData => new(mode == Mode.Install ? "Download24" : "Delete24", GetInstallUninstallButtonText(), true, Command);

    public ButtonData OpenButtonData
    {
        get
        {
            if (string.IsNullOrWhiteSpace(pluginMetadata.ProfileUri?.ToString()))
            {
                return new("Folder24", (string)App.LanguageDictionary["folder"], path is not null, new CommandHandler(() => Process.Start("explorer.exe", path!)));
            }
            else
            {
                return new("Open24", "mod.io", true, new CommandHandler(OpenModIoPage));
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

    private void OpenModIoPage()
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
            Mode.Install => (string)App.LanguageDictionary["install"],
            Mode.Uninstall => (string)App.LanguageDictionary["uninstall"],
            _ => throw new NotImplementedException()
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record ButtonData(string Icon, string Text, bool IsEnabled, ICommand Command);

    public enum Mode
    {
        Install,
        Uninstall
    }
}