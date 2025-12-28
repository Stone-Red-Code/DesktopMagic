using CuteUtils.Logging;

using System;
using System.IO;
using System.Threading;
using System.Windows;

using Wpf.Ui;

namespace DesktopMagic;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public const string AppGuid = "{{61FE5CE9-47C3-4255-A1F4-5BCF4ACA0879}";

    public const string AppName = "DesktopMagic";

    // This is the previous name of the application, used to migrate the application data folder
    private const string PreviousAppName = "Desktop Magic";

    private static readonly string logFilePath = Path.Combine(ApplicationDataPath, $"{AppName}.log");
    private readonly Thread? eventThread;
    private readonly EventWaitHandle eventWaitHandle;
    public static string ApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", AppName);

    public static string PluginsPath => Path.Combine(ApplicationDataPath, "Plugins");

    public static IContentDialogService DialogService { get; } = new ContentDialogService();

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

    public static ResourceDictionary LanguageDictionary
    {
        get
        {
            ResourceDictionary dict = [];
            string currentCulture = Thread.CurrentThread.CurrentUICulture.ToString();

            if (currentCulture.Contains("de"))
            {
                dict.Source = new Uri("..\\Resources\\Strings\\StringResources.de.xaml", UriKind.Relative);
            }
            else
            {
                dict.Source = new Uri("..\\Resources\\Strings\\StringResources.en.xaml", UriKind.Relative);
            }

            return dict;
        }
    }

    private static string PreviousApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", PreviousAppName);

    public App()
    {
        // Setup global event handler
        eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, AppGuid, out bool createdNew);

        // Check if creating new was successful
        if (!createdNew)
        {
            Setup(false);
            Logger.LogWarn("Shutting down because other instance already running.", source: "Setup");
            // Shutdown Application
            _ = eventWaitHandle.Set();
            Current.Shutdown();
        }
        else
        {
            eventThread = new Thread(() =>
            {
                while (eventWaitHandle.WaitOne())
                {
                    _ = Current.Dispatcher.BeginInvoke(() => ((MainWindow)Current.MainWindow).RestoreWindow());
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

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception exception = (Exception)e.ExceptionObject;
        Logger.LogFatal(exception + (e.IsTerminating ? "\t Process terminating!" : ""), source: exception.Source ?? "Unknown");
    }

    private static async void Setup(bool clearLogFile)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            if (Directory.Exists(PreviousApplicationDataPath) && !Directory.Exists(ApplicationDataPath))
            {
                Directory.Move(PreviousApplicationDataPath, ApplicationDataPath);
                Logger.LogInfo("Migrated ApplicationData folder", source: "Setup");
            }

            if (!Directory.Exists(ApplicationDataPath))
            {
                _ = Directory.CreateDirectory(ApplicationDataPath);
                Logger.LogInfo("Created ApplicationData folder", source: "Setup");
            }

            if (!Directory.Exists(PluginsPath))
            {
                _ = Directory.CreateDirectory(PluginsPath);
                Logger.LogInfo("Created Plugins folder", source: "Setup");
            }
        }
        catch (Exception ex)
        {
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = AppName,
                Content = ex.ToString(),
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
            Logger.Log(ex.Message, "Setup", LogSeverity.Error);
        }

        if (clearLogFile)
        {
            Logger.ClearLogFile(LogSeverity.Info);
        }

        Logger.LogInfo("Setup complete", source: "Setup");
    }
}