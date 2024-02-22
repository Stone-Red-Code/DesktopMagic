global using Stone_Red_Utilities.Logging;

using Stone_Red_C_Sharp_Utilities.Logging;

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
    private readonly string logFilePath;

    private readonly Thread? eventThread;
    private readonly EventWaitHandle eventWaitHandle;

    private readonly Logger logger = new Logger();
    public static string ApplicationDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", AppName);

    public static Logger Logger => ((App)Current).logger;

    public App()
    {
        logFilePath = ApplicationDataPath + "\\log.log";

        // Setup global event handler
        eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, AppGuid, out bool createdNew);

        //check if creating new was successful
        if (!createdNew)
        {
            Setup(false);
            Logger.Log("Shutting down because other instance already running.", "Setup", LogSeverity.Warn);
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
                DebugConsoleFormat = $"> {{{LogFormatType.DateTime}:HH:mm:ss}} | {{{LogFormatType.LogSeverity},-5}} | {{{LogFormatType.Message}}}\nat {{{LogFormatType.LineNumber}}} | {{{LogFormatType.FilePath}}}"
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