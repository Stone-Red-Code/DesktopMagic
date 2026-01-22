using DesktopMagic.Helpers;
using DesktopMagic.Plugins;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using Wpf.Ui.Controls;

namespace DesktopMagic.DataContexts;

internal class PluginEntryDataContext(PluginMetadata pluginMetadata, ICommand command, PluginEntryDataContext.Mode mode, string? path = null, string? csprojPath = null) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isVisible = true;

    public string Name => pluginMetadata.Name;

    public string? Description => pluginMetadata.Description?.Trim();

    public string Author => pluginMetadata.Author ?? (string)App.LanguageDictionary["unknown"];

    public string? Version => pluginMetadata.Version ?? (string)App.LanguageDictionary["unknown"];

    public string? Logo => pluginMetadata.IconUri?.ToString();

    public DateTime? DateAdded => pluginMetadata.Added;
    public DateTime? DateUpdated => pluginMetadata.Updated;

    public uint Id => pluginMetadata.Id;

    public string? Path => path;

    public bool IsLocalPlugin => pluginMetadata.IsLocalPlugin;

    public ICommand Command => command;

    public ButtonData InstallUninstallButtonData
    {
        get
        {
            if (mode == Mode.Install)
            {
                return new(new SymbolIcon(SymbolRegular.ArrowDownload24), GetInstallUninstallButtonText(), true, Command);
            }
            else
            {
                return new(new SymbolIcon(SymbolRegular.Delete24), GetInstallUninstallButtonText(), true, Command);
            }
        }
    }

    public ButtonData OpenButtonData
    {
        get
        {
            if (File.Exists(csprojPath))
            {
                return new(new SymbolIcon(SymbolRegular.Code24), "IDE", true, new CommandHandler(OpenCsprojInIDE));
            }
            else if (string.IsNullOrWhiteSpace(pluginMetadata.ProfileUri?.ToString()))
            {
                return new(new SymbolIcon(SymbolRegular.Folder24), (string)App.LanguageDictionary["folder"], path is not null, new CommandHandler(() => Process.Start("explorer.exe", path!)));
            }
            else
            {
                return new(new SymbolIcon(SymbolRegular.Open24), "mod.io", true, new CommandHandler(OpenModIoPage));
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

    private void OpenCsprojInIDE()
    {
        string? associatedProgram = FileUtilities.GetAssociatedProgram(".csproj");
        string pluginProjectPath = System.IO.Path.GetDirectoryName(csprojPath)!;

        if (string.IsNullOrWhiteSpace(pluginProjectPath))
        {
            App.Logger.LogError("Could not determine plugin project directory.", source: "PluginManager");
            return;
        }

        if (associatedProgram is null)
        {
            App.Logger.LogInfo($"Opening plugin project in Explorer: {pluginProjectPath}", source: "PluginManager");
            _ = Process.Start("explorer.exe", pluginProjectPath);
            return;
        }

        App.Logger.LogInfo($"Opening plugin project in IDE: {associatedProgram}", source: "PluginManager");
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = associatedProgram,
            Arguments = csprojPath,
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

    public record ButtonData(SymbolIcon Icon, string Text, bool IsEnabled, ICommand Command);

    public enum Mode
    {
        Install,
        Uninstall
    }
}