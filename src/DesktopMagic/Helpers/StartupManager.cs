using IWshRuntimeLibrary;

using System;
using System.IO;
using System.Reflection;

namespace DesktopMagic.Helpers;

public enum AutoStartStatus
{
    Success = 0,
    InvalidParameters = 1,
    AlreadyEnabled = 2,
    AlreadyDisabled = 3,
    UnexpectedError = 4
}

public static class StartupManager
{
    private static readonly string appPath = Path.ChangeExtension(Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException("Failed to get the application path."), ".exe");
    private static readonly string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
    private static readonly string shortcutPath = Path.Combine(startupFolder, Path.GetFileNameWithoutExtension(appPath) + ".lnk");

    public static bool IsAutoStartEnabled()
    {
        return System.IO.File.Exists(shortcutPath);
    }

    public static AutoStartStatus EnableAutoStart()
    {
        if (IsAutoStartEnabled())
        {
            return AutoStartStatus.AlreadyEnabled;
        }

        try
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = appPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(appPath);
            shortcut.Save();
            return AutoStartStatus.Success;
        }
        catch
        {
            return AutoStartStatus.UnexpectedError;
        }
    }

    public static AutoStartStatus DisableAutoStart()
    {
        if (!IsAutoStartEnabled())
        {
            return AutoStartStatus.AlreadyDisabled;
        }

        try
        {
            System.IO.File.Delete(shortcutPath);
            return AutoStartStatus.Success;
        }
        catch
        {
            return AutoStartStatus.UnexpectedError;
        }
    }

    public static async void ToggleAutoStart()
    {
        AutoStartStatus status = IsAutoStartEnabled() ? DisableAutoStart() : EnableAutoStart();

        string message = status switch
        {
            AutoStartStatus.Success => "Auto-start setting changed successfully.",
            AutoStartStatus.AlreadyEnabled => "Auto-start is already enabled.",
            AutoStartStatus.AlreadyDisabled => "Auto-start is already disabled.",
            _ => "Failed to change auto-start setting.",
        };

        Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = App.AppName,
            Content = message,
            CloseButtonText = "Ok"
        };
        _ = await messageBox.ShowDialogAsync();
    }
}