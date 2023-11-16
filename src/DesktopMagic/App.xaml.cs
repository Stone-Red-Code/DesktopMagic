﻿global using Stone_Red_Utilities.Logging;

using AlwaysUpToDate;

using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace DesktopMagic;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public const string AppName = "Desktop Magic";
    private readonly string logFilePath;
    private readonly Mutex _mutex;
#if DEBUG
    private readonly Updater updater = new Updater(TimeSpan.FromDays(1), "https://raw.githubusercontent.com/Stone-Red-Code/DesktopMagic/develop/update/updateInfo.json");
#else
    private readonly Updater updater = new Updater(TimeSpan.FromDays(1), "https://raw.githubusercontent.com/Stone-Red-Code/DesktopMagic/main/update/updateInfo.json");
#endif
    public static string ApplicationDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", AppName);

    public static Logger Logger { get; } = new Logger();

    public App()
    {
        logFilePath = ApplicationDataPath + "\\Log.log";

        // Try to grab mutex
        _mutex = new Mutex(true, $"Stone_Red{AppName}", out bool createdNew);

        //check if creating new was successful
        if (!createdNew)
        {
            Setup(false);
            Logger.Log("Shutting down because other instance already running.", "Setup");
            //Shutdown Application
            Current.Shutdown();
        }
        else
        {
            Setup(true);
            Exit += CloseMutexHandler;

            updater.ProgressChanged += Updater_ProgressChanged;
            updater.OnException += Updater_OnException;
            updater.NoUpdateAvailible += Updater_NoUpdateAvailible;
            updater.UpdateAvailible += Updater_UpdateAvailible;
            updater.Start();
        }
    }

    protected void CloseMutexHandler(object sender, EventArgs e)
    {
        _mutex?.Close();
    }

    private void Updater_UpdateAvailible(string version, string additionalInformation)
    {
        updater.Update();
    }

    private void Updater_NoUpdateAvailible()
    {
        Logger.Log("No update available", "Updater");
    }

    private void Updater_OnException(Exception exception)
    {
        Logger.Log(exception.ToString(), "Updater");
    }

    private void Updater_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
    {
        Logger.Log($"Downloading: {progressPercentage}% {totalBytesDownloaded}/{totalFileSize}", "Updater");
    }

    private void Setup(bool clearLogFile)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        Logger.Config = new LogConfig()
        {
            FatalConfig = new OutputConfig()
            {
                Color = ConsoleColor.DarkRed,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            ErrorConfig = new OutputConfig()
            {
                Color = ConsoleColor.Red,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            WarnConfig = new OutputConfig()
            {
                Color = ConsoleColor.Yellow,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            InfoConfig = new OutputConfig()
            {
                Color = ConsoleColor.White,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            DebugConfig = new OutputConfig()
            {
                Color = ConsoleColor.Gray,
                LogTarget = LogTarget.DebugConsole,
            },
            FormatConfig = new FormatConfig()
            {
                DebugConsoleFormat = $"> {{{LogFormatType.DateTime}:hh:mm:ss}} | {{{LogFormatType.LogSeverity},-5}} | {{{LogFormatType.Message}}}\nat {{{LogFormatType.LineNumber}}} | {{{LogFormatType.FilePath}}}"
            }
        };

        try
        {
            if (!Directory.Exists(ApplicationDataPath))
            {
                _ = Directory.CreateDirectory(ApplicationDataPath);
            }
            Logger.Log("Created ApplicationData Folder", "Main");
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show(ex.ToString());
            Logger.Log(ex.Message, "Setup", LogSeverity.Error);
        }

        if (clearLogFile)
        {
            Logger.ClearLogFile(LogSeverity.Info);
        }

        Logger.Log("Log setup complete", "Setup");
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception exception = (Exception)e.ExceptionObject;
        Logger.Log(exception + (e.IsTerminating ? "\t Process terminating!" : ""), exception.Source, LogSeverity.Fatal);
    }
}