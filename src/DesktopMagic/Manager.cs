using DesktopMagic.BuiltInPlugins;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace DesktopMagic;

/// <summary>
/// Singleton Manager class that handles global state, plugin management, and shared operations
/// </summary>
public sealed class Manager
{
    private static Manager? _instance;
    private static readonly object _lock = new();

    public static Manager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new Manager();
                }
            }
            return _instance;
        }
    }

    // Plugin management
    private readonly Dictionary<uint, InternalPluginData> _plugins = [];
    private readonly Dictionary<PluginMetadata, Type> _builtInPlugins = new()
    {
        {new((string)App.LanguageDictionary["musicVisualizer"], 1) { Author = "Stone_Red" }, typeof(MusicVisualizerPlugin)},
        {new((string)App.LanguageDictionary["time"],2) { Author = "Stone_Red" }, typeof(TimePlugin)},
        {new((string)App.LanguageDictionary["date"],3) { Author = "Stone_Red" }, typeof(DatePlugin)},
        {new((string)App.LanguageDictionary["cpuUsage"], 4) { Author = "Stone_Red" }, typeof(CpuMonitorPlugin)},
        {new((string)App.LanguageDictionary["weather"], 5) { Author = "Stone_Red" }, typeof(WeatherPlugin)},
    };

    // Window management
    public List<IPluginWindow> PluginWindows { get; } = [];
    
    public IReadOnlyDictionary<uint, InternalPluginData> Plugins => _plugins;

    public bool BlockWindowsClosing { get; set; } = true;

    // Edit mode tracking
    private bool _isEditMode = false;
    public bool IsEditMode => _isEditMode;

    // Settings
    public DesktopMagicSettings Settings { get; set; } = new();
    public bool IsLoaded { get; set; } = false;

    private readonly JsonSerializerOptions _jsonSettingsOptions = new()
    {
        Converters = { new ColorJsonConverter() }
    };

    // Events
    public event Action? PluginsChanged;
    public event Action? SettingsChanged;
    public event Action<bool>? EditModeChanged;

    private Manager()
    {
        // Private constructor for singleton
    }

    #region Plugin Management

    public Dictionary<uint, InternalPluginData> GetPlugins() => new(_plugins);

    public void LoadPlugins()
    {
        App.Logger.LogInfo("Loading plugins", source: "Manager");
        _plugins.Clear();

        // Load built-in plugins
        foreach (var builtInPlugin in _builtInPlugins.Keys)
        {
            _plugins.Add(builtInPlugin.Id, new(builtInPlugin, PluginType.DotNet, string.Empty));
        }

        // Load external plugins
        foreach (string directory in Directory.GetDirectories(App.PluginsPath))
        {
            string? pluginDllPath = Directory.GetFiles(directory, "main.dll").FirstOrDefault();
            string? pluginHtmlPath = Directory.GetFiles(directory, "main.html").FirstOrDefault();
            string? pluginMetadataPath = Directory.GetFiles(directory, "metadata.json").FirstOrDefault();

            if (pluginDllPath is null && pluginHtmlPath is null)
            {
                App.Logger.LogError($"Plugin \"{directory}\" has no \"main.dll\" or \"main.html\"", source: "Manager");
                continue;
            }

            if (pluginMetadataPath is null)
            {
                App.Logger.LogWarn($"Plugin \"{directory}\" has no \"metadata.json\"", source: "Manager");
                continue;
            }

            PluginMetadata? pluginMetadata = JsonSerializer.Deserialize<PluginMetadata>(File.ReadAllText(pluginMetadataPath));

            if (pluginMetadata is null)
            {
                App.Logger.LogError($"Plugin \"{directory}\" has no valid \"metadata.json\"", source: "Manager");
                continue;
            }

            if (_plugins.ContainsKey(pluginMetadata.Id))
            {
                App.Logger.LogError($"Plugin \"{directory}\" has the same id as another plugin", source: "Manager");
                continue;
            }

            PluginType pluginType = pluginHtmlPath is not null ? PluginType.Web : PluginType.DotNet;

            _plugins.Add(pluginMetadata.Id, new(pluginMetadata, pluginType, directory));
        }

        PluginsChanged?.Invoke();
        App.Logger.LogInfo($"Loaded {_plugins.Count} plugins", source: "Manager");
    }

    public void LoadPlugin(uint pluginId, Action<InternalPluginData>? onPluginLoaded = null)
    {
        if (!_plugins.TryGetValue(pluginId, out InternalPluginData? internalPluginData))
        {
            return;
        }

        if (!Settings.CurrentLayout.Plugins.TryGetValue(pluginId, out PluginSettings? pluginSettings))
        {
            pluginSettings = new PluginSettings();
            Settings.CurrentLayout.Plugins.Add(pluginId, pluginSettings);
        }

        IPluginWindow? existingWindow = PluginWindows.FirstOrDefault(w => w.PluginMetadata.Id == internalPluginData.Metadata.Id);

        if (existingWindow is not null || !pluginSettings.Enabled)
        {
            // Close the window if it's already open or disabled
            if (existingWindow is not null)
            {
                try
                {
                    BlockWindowsClosing = false;
                    existingWindow.Close();
                    BlockWindowsClosing = true;
                    PluginWindows.Remove(existingWindow);
                }
                catch (Exception ex)
                {
                    App.Logger.LogError(ex.Message, source: "Manager");
                }
            }
            return;
        }

        IPluginWindow window;

        if (_builtInPlugins.TryGetValue(internalPluginData.Metadata, out Type? pluginType))
        {
            window = new PluginWindow((Api.Plugin)Activator.CreateInstance(pluginType)!, internalPluginData.Metadata, pluginSettings)
            {
                Title = internalPluginData.Metadata.Id.ToString()
            };
        }
        else if (internalPluginData.Type == PluginType.Web)
        {
            window = new WebPluginWindow(internalPluginData.Metadata, pluginSettings, internalPluginData.DirectoryPath)
            {
                Title = internalPluginData.Metadata.Id.ToString()
            };
        }
        else
        {
            window = new PluginWindow(internalPluginData.Metadata, pluginSettings, internalPluginData.DirectoryPath)
            {
                Title = internalPluginData.Metadata.Id.ToString()
            };
        }

        Action? pluginLoadedHandler = null;
        pluginLoadedHandler = () =>
        {
            onPluginLoaded?.Invoke(internalPluginData);
            window.PluginLoaded -= pluginLoadedHandler;
        };

        Action exitHandler = () =>
        {
            PluginWindows.Remove(window);
            BlockWindowsClosing = false;
            window.Close();
            BlockWindowsClosing = true;
            pluginSettings.Enabled = false;
        };

        window.PluginLoaded += pluginLoadedHandler;
        window.OnExit += exitHandler;

        window.Show();
        window.SetEditMode(_isEditMode);

        PluginWindows.Add(window);
    }

    public void SetEditMode(bool editMode)
    {
        _isEditMode = editMode;
        foreach (IPluginWindow window in PluginWindows)
        {
            window.SetEditMode(editMode);
        }
        EditModeChanged?.Invoke(editMode);
        SaveSettings();
    }

    #endregion

    #region Settings Management

    public void LoadSettings()
    {
        App.Logger.LogInfo("Loading settings", source: "Manager");

        if (!File.Exists(Path.Combine(App.ApplicationDataPath, "settings.json")))
        {
            Settings = new DesktopMagicSettings();
            Settings.Layouts.Add(new Layout("Default"));
            Settings.Themes.Add(new Theme("Default"));
            return;
        }

        string json = File.ReadAllText(Path.Combine(App.ApplicationDataPath, "settings.json"));
        Settings = JsonSerializer.Deserialize<DesktopMagicSettings>(json, _jsonSettingsOptions) ?? new DesktopMagicSettings();

        if (Settings.Layouts.Count == 0)
        {
            Settings.Layouts.Add(new Layout("Default"));
        }

        if (Settings.Themes.Count == 0)
        {
            Settings.Themes.Add(new Theme("Default"));
        }

        SettingsChanged?.Invoke();
    }

    public void SaveSettings()
    {
        if (!IsLoaded)
        {
            return;
        }

        string json = JsonSerializer.Serialize(Settings, _jsonSettingsOptions);
        File.WriteAllText(Path.Combine(App.ApplicationDataPath, "settings.json"), json);
        App.Logger.LogInfo("Settings saved", source: "Manager");
        SettingsChanged?.Invoke();
    }

    #endregion

    #region Layout Management

    public void LoadLayout(bool minimize = true, Action? onComplete = null)
    {
        App.Logger.LogInfo("Loading layout", source: "Manager");
        BlockWindowsClosing = false;

        foreach (IPluginWindow window in PluginWindows)
        {
            window.Close();
        }

        BlockWindowsClosing = true;
        PluginWindows.Clear();

        bool showWindow = true;

        // Load plugins
        foreach (uint pluginId in _plugins.Keys)
        {
            InternalPluginData internalPluginData = _plugins[pluginId];

            // Add plugin to layout if it doesn't exist
            if (!Settings.CurrentLayout.Plugins.TryGetValue(pluginId, out PluginSettings? pluginSettings))
            {
                Settings.CurrentLayout.Plugins.Add(pluginId, new PluginSettings() { Metadata = internalPluginData.Metadata });
                continue;
            }

            pluginSettings.Metadata = internalPluginData.Metadata;

            if (pluginSettings.Enabled)
            {
                LoadPlugin(pluginId);
            }

            if (showWindow && pluginSettings.Enabled)
            {
                showWindow = false;
            }
        }

        // Remove plugins that are not loaded anymore
        var pluginIdsToRemove = Settings.CurrentLayout.Plugins.Keys.Where(id => !_plugins.ContainsKey(id)).ToList();
        foreach (uint pluginId in pluginIdsToRemove)
        {
            Settings.CurrentLayout.Plugins.Remove(pluginId);
        }

        Settings.CurrentLayout.UpdatePlugins();

        if (minimize && !showWindow)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            Application.Current.MainWindow.ShowInTaskbar = false;
        }

        onComplete?.Invoke();
        App.Logger.LogInfo("Layout loaded", source: "Manager");
    }

    #endregion

    #region Cleanup

    public void CloseAllPluginWindows()
    {
        foreach (IPluginWindow window in PluginWindows)
        {
            window.Hide();
        }
    }

    #endregion
}

public class InternalPluginData(PluginMetadata pluginMetadata, PluginType pluginType, string directoryPath)
{
    public PluginMetadata Metadata { get; set; } = pluginMetadata;
    public PluginType Type { get; set; } = pluginType;
    public string DirectoryPath { get; set; } = directoryPath;
}

public enum PluginType
{
    DotNet,
    Web
}