using CuteUtils.Logging;

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
    public const string AppGuid = "{{61FE5CE9-47C3-4255-A1F4-5BCF4ACA0879}";

    public const string AppName = "Desktop Magic";

    private static readonly string logFilePath = Path.Combine(ApplicationDataPath, $"{AppName}.log");

    private readonly Thread? eventThread;

    private readonly EventWaitHandle eventWaitHandle;

    public static Logger Logger { get; } = new Logger()
    {
        Config = new()
        {
            FatalConfig = new OutputConfig()
            {
                ConsoleColor = ConsoleColor.DarkRed,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            ErrorConfig = new OutputConfig()
            {
                ConsoleColor = ConsoleColor.Red,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            WarnConfig = new OutputConfig()
            {
                ConsoleColor = ConsoleColor.Yellow,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            InfoConfig = new OutputConfig()
            {
                ConsoleColor = ConsoleColor.White,
                LogTarget = LogTarget.DebugConsole | LogTarget.File,
                FilePath = logFilePath
            },
            DebugConfig = new OutputConfig()
            {
                ConsoleColor = ConsoleColor.Gray,
                LogTarget = LogTarget.DebugConsole,
            },
            FormatConfig = new FormatConfig()
            {
                DebugConsoleFormat = $"> {{{LogFormatType.DateTime}:HH:mm:ss}} | {{{LogFormatType.LogSeverity},-5}} | {{{LogFormatType.Message}}}\nat {{{LogFormatType.LineNumber}}} | {{{LogFormatType.FilePath}}}"
            }
        }
    };

    public static string ApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", AppName);

    public App()
    {
        // Setup global event handler
        eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, AppGuid, out bool createdNew);

        //check if creating new was successful
        if (!createdNew)
        {
            Setup(false);
            Logger.LogWarn("Shutting down because other instance already running.", source: "Setup");
            //Shutdown Application
            _ = eventWaitHandle.Set();
            Current.Shutdown();
        }
        else
        {
            eventThread = new Thread(
                () =>
                {
                    while (eventWaitHandle.WaitOne())
                    {
                        _ = Current.Dispatcher.BeginInvoke(
                            () => ((MainWindow)Current.MainWindow).RestoreWindow());
                    }
                });
            eventThread.Start();

            Setup(true);
            Exit += CloseHandler;
        }
    }

    protected void CloseHandler(object sender, EventArgs e)
    {
        eventWaitHandle.Close();
        eventThread?.Interrupt();
    }

    private void Setup(bool clearLogFile)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            if (!Directory.Exists(ApplicationDataPath))
            {
                _ = Directory.CreateDirectory(ApplicationDataPath);
            }
            Logger.LogInfo("Created ApplicationData Folder", source: "Main");
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

        Logger.LogInfo("Setup complete", source: "Setup");
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception exception = (Exception)e.ExceptionObject;
        Logger.LogFatal(exception + (e.IsTerminating ? "\t Process terminating!" : ""), source: exception.Source ?? "Unknown");
    }
}