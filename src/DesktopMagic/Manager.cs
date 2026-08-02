using DesktopMagic.BuiltInPlugins;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System;
using System.Collections.Generic;
using System.Drawing;
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

    // Name of the built-in layout that shows no widgets on a screen.
    public const string EmptyLayoutName = "Empty";

    // Plugin management
    private readonly Dictionary<uint, InternalPluginData> _plugins = [];
    private readonly Dictionary<PluginMetadata, Type> _builtInPlugins = new()
    {
        {new((string)App.LanguageDictionary["musicVisualizer"], 1) { Author = "Stone_Red" }, typeof(MusicVisualizerPlugin)},
        {new((string)App.LanguageDictionary["time"],2) { Author = "Stone_Red" }, typeof(TimePlugin)},
        {new((string)App.LanguageDictionary["date"],3) { Author = "Stone_Red" }, typeof(DatePlugin)},
        {new((string)App.LanguageDictionary["cpuUsage"], 4) { Author = "Stone_Red" }, typeof(CpuMonitorPlugin)},
        {new((string)App.LanguageDictionary["weather"], 5) { Author = "Stone_Red" }, typeof(WeatherPlugin)},
        {new((string)App.LanguageDictionary["nextMeetingCountdown"], 6) { Author = "Stone_Red" }, typeof(NextMeetingCountdownPlugin)},
        {new((string)App.LanguageDictionary["agenda"], 7) { Author = "Stone_Red" }, typeof(AgendaPlugin)},
        {new((string)App.LanguageDictionary["benchmark"], 8) { Author = "Stone_Red" }, typeof(BenchmarkPlugin)},
        {new((string)App.LanguageDictionary["skiaBenchmark"], 9) { Author = "Stone_Red" }, typeof(SkiaBenchmarkPlugin)},
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

    // Screen selection (which screen is currently edited in the UI)
    public string? SelectedScreenDeviceName { get; set; }

    public System.Windows.Forms.Screen SelectedScreen => ScreenUtilities.GetScreenByDeviceName(SelectedScreenDeviceName) ?? ScreenUtilities.GetPrimaryScreen();

    public Layout SelectedLayout => GetLayoutForScreen(SelectedScreen);

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
        foreach (PluginMetadata builtInPlugin in _builtInPlugins.Keys)
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

    /// <summary>
    /// Loads (or unloads) the given plugin on every screen that currently uses the given layout.
    /// This keeps screens sharing a layout in sync when plugins are enabled or disabled.
    /// </summary>
    public void LoadPlugin(uint pluginId, Layout layout, Action<InternalPluginData>? onPluginLoaded = null)
    {
        if (!_plugins.TryGetValue(pluginId, out InternalPluginData? internalPluginData))
        {
            return;
        }

        if (!layout.Plugins.TryGetValue(pluginId, out PluginSettings? pluginSettings))
        {
            pluginSettings = new PluginSettings();
            layout.Plugins.Add(pluginId, pluginSettings);
        }

        pluginSettings.Metadata = internalPluginData.Metadata;
        pluginSettings.Owner = layout;

        foreach (System.Windows.Forms.Screen screen in ScreenUtilities.GetAllScreens())
        {
            if (GetLayoutForScreen(screen) == layout)
            {
                EnsurePluginWindow(screen, layout, internalPluginData, pluginSettings, onPluginLoaded);
            }
        }

        layout.UpdatePlugins();
    }

    /// <summary>
    /// Creates or closes the plugin window for a single screen, based on the plugin settings.
    /// </summary>
    private void EnsurePluginWindow(System.Windows.Forms.Screen screen, Layout layout, InternalPluginData internalPluginData, PluginSettings pluginSettings, Action<InternalPluginData>? onPluginLoaded)
    {
        string screenDeviceName = screen.DeviceName;
        Rectangle screenBounds = screen.Bounds;

        IPluginWindow? existingWindow = PluginWindows.FirstOrDefault(w => w.PluginMetadata.Id == internalPluginData.Metadata.Id && w.ScreenDeviceName == screenDeviceName);

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
            window = new PluginWindow((Api.Plugin)Activator.CreateInstance(pluginType)!, internalPluginData.Metadata, pluginSettings, screenBounds, screenDeviceName)
            {
                Title = internalPluginData.Metadata.Id.ToString()
            };
        }
        else if (internalPluginData.Type == PluginType.Web)
        {
            window = new WebPluginWindow(internalPluginData.Metadata, pluginSettings, internalPluginData.DirectoryPath, screenBounds, screenDeviceName)
            {
                Title = internalPluginData.Metadata.Id.ToString()
            };
        }
        else
        {
            window = new PluginWindow(internalPluginData.Metadata, pluginSettings, internalPluginData.DirectoryPath, screenBounds, screenDeviceName)
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
            // Close the widget on every screen using this layout
            foreach (System.Windows.Forms.Screen sharedScreen in ScreenUtilities.GetAllScreens())
            {
                if (GetLayoutForScreen(sharedScreen) == layout)
                {
                    IPluginWindow? sharedWindow = PluginWindows.FirstOrDefault(w => w.PluginMetadata.Id == internalPluginData.Metadata.Id && w.ScreenDeviceName == sharedScreen.DeviceName);
                    if (sharedWindow is not null)
                    {
                        PluginWindows.Remove(sharedWindow);
                        BlockWindowsClosing = false;
                        sharedWindow.Close();
                        BlockWindowsClosing = true;
                    }
                }
            }
            pluginSettings.Enabled = false;
        };

        window.PluginLoaded += pluginLoadedHandler;
        window.OnExit += exitHandler;

        window.Show();
        window.SetEditMode(_isEditMode);

        PluginWindows.Add(window);
    }

    public void ReloadPlugins()
    {
        App.Logger.LogInfo("Reloading plugins", source: "PluginManager");

        LoadPlugins();
        LoadLayout();
    }

    public void SetEditMode(bool editMode)
    {
        _isEditMode = editMode;
        foreach (IPluginWindow window in PluginWindows)
        {
            window.SetEditMode(editMode);
        }

        Application.Current.MainWindow.Topmost = editMode;

        EditModeChanged?.Invoke(editMode);
        SaveSettings();
    }

    private readonly Dictionary<(Layout, uint), SettingSynchronizer> settingSynchronizers = [];

    /// <summary>
    /// Gets (or creates) the synchronizer that keeps the settings of all windows showing the
    /// given plugin in the given layout in sync.
    /// </summary>
    internal SettingSynchronizer GetSettingSynchronizer(Layout layout, uint pluginId)
    {
        (Layout, uint) key = (layout, pluginId);
        if (!settingSynchronizers.TryGetValue(key, out SettingSynchronizer? synchronizer))
        {
            synchronizer = new SettingSynchronizer();
            settingSynchronizers.Add(key, synchronizer);
        }

        return synchronizer;
    }

    /// <summary>
    /// Drops the synchronizer for the given plugin in the given layout once no windows use it anymore.
    /// </summary>
    internal void ReleaseSettingSynchronizer(Layout layout, uint pluginId)
    {
        _ = settingSynchronizers.Remove((layout, pluginId));
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
            Settings.Layouts.Add(new Layout(EmptyLayoutName));
            Settings.Themes.Add(new Theme("Default"));
            Settings.SchemaVersion = 1;
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

        if (Settings.SchemaVersion < 1)
        {
            MigrateToScreenAwareSettings();
            Settings.SchemaVersion = 1;
        }

        if (!Settings.Layouts.Any(layout => layout.Name == EmptyLayoutName))
        {
            Settings.Layouts.Add(new Layout(EmptyLayoutName));
        }

        SettingsChanged?.Invoke();
    }

    /// <summary>
    /// Migrates legacy settings to screen-aware layouts:
    /// records the primary screen aspect ratio on each layout and converts
    /// absolute pixel positions/sizes to percentages of the primary screen bounds.
    /// </summary>
    private void MigrateToScreenAwareSettings()
    {
        App.Logger.LogInfo("Migrating settings to screen-aware layouts", source: "Manager");

        System.Windows.Forms.Screen primaryScreen = ScreenUtilities.GetPrimaryScreen();
        double aspectRatio = ScreenUtilities.GetAspectRatio(primaryScreen);
        Rectangle bounds = primaryScreen.Bounds;

        foreach (Layout layout in Settings.Layouts)
        {
            if (layout.ScreenAspectRatio <= 0)
            {
                layout.ScreenAspectRatio = aspectRatio;
            }

            foreach (PluginSettings plugin in layout.Plugins.Values)
            {
                if (plugin.Position.X > 1 || plugin.Position.Y > 1 || plugin.Position.X < 0 || plugin.Position.Y < 0)
                {
                    plugin.Position = ScreenUtilities.PositionToPercent(new System.Windows.Point(plugin.Position.X, plugin.Position.Y), bounds);
                }

                if (plugin.Size.X > 1 || plugin.Size.Y > 1)
                {
                    plugin.Size = ScreenUtilities.SizeToPercent(new System.Windows.Point(plugin.Size.X, plugin.Size.Y), bounds);
                }
            }
        }

        if (Settings.ScreenLayouts.Count == 0)
        {
            Settings.ScreenLayouts[primaryScreen.DeviceName] = Settings.CurrentLayoutName ?? "Default";
        }

        App.Logger.LogInfo("Settings migrated to screen-aware layouts", source: "Manager");
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

    /// <summary>
    /// Resolves the layout that should be applied to the given screen:
    /// 1. the layout explicitly bound to this screen's device name,
    /// 2. the layout with the closest matching aspect ratio,
    /// 3. the first layout.
    /// </summary>
    public Layout GetLayoutForScreen(System.Windows.Forms.Screen screen)
    {
        if (Settings.ScreenLayouts.TryGetValue(screen.DeviceName, out string? layoutName))
        {
            Layout? bound = Settings.Layouts.FirstOrDefault(layout => layout.Name == layoutName);
            if (bound is not null)
            {
                return bound;
            }
        }

        double targetRatio = ScreenUtilities.GetAspectRatio(screen);
        Layout? byAspectRatio = Settings.Layouts
            .Where(layout => layout.ScreenAspectRatio > 0)
            .OrderBy(layout => Math.Abs(layout.ScreenAspectRatio - targetRatio))
            .FirstOrDefault();

        if (byAspectRatio is not null)
        {
            return byAspectRatio;
        }

        return Settings.Layouts.FirstOrDefault() ?? new Layout("ERROR");
    }

    /// <summary>
    /// Binds the given layout to the given screen on this machine.
    /// </summary>
    public void BindLayoutToScreen(System.Windows.Forms.Screen screen, Layout layout)
    {
        Settings.ScreenLayouts[screen.DeviceName] = layout.Name;
        SaveSettings();
    }

    /// <summary>
    /// Gets all plugin windows currently shown on the given screen.
    /// </summary>
    public IEnumerable<IPluginWindow> GetWindowsForScreen(string screenDeviceName)
    {
        return PluginWindows.Where(window => window.ScreenDeviceName == screenDeviceName).ToList();
    }

    /// <summary>
    /// Loads all screens' layouts at once, opening the enabled widgets of every screen.
    /// </summary>
    public void LoadLayout(Action? onComplete = null)
    {
        App.Logger.LogInfo("Loading layouts", source: "Manager");
        BlockWindowsClosing = false;

        foreach (IPluginWindow window in PluginWindows)
        {
            window.Close();
        }

        BlockWindowsClosing = true;
        PluginWindows.Clear();

        foreach (System.Windows.Forms.Screen screen in ScreenUtilities.GetAllScreens())
        {
            Layout layout = GetLayoutForScreen(screen);
            LoadScreen(screen, layout);
        }

        onComplete?.Invoke();
        App.Logger.LogInfo("Layouts loaded", source: "Manager");
    }

    /// <summary>
    /// Reloads the widgets of a single screen using the layout currently bound to it.
    /// </summary>
    public void ReloadScreen(System.Windows.Forms.Screen screen)
    {
        App.Logger.LogInfo($"Reloading screen {screen.DeviceName}", source: "Manager");

        List<IPluginWindow> windows = PluginWindows.Where(window => window.ScreenDeviceName == screen.DeviceName).ToList();

        BlockWindowsClosing = false;
        foreach (IPluginWindow window in windows)
        {
            window.Close();
        }

        BlockWindowsClosing = true;
        PluginWindows.RemoveAll(window => windows.Contains(window));

        Layout layout = GetLayoutForScreen(screen);
        LoadScreen(screen, layout);
    }

    private void LoadScreen(System.Windows.Forms.Screen screen, Layout layout)
    {
        App.Logger.LogInfo($"Loading layout \"{layout.Name}\" for screen {screen.DeviceName}", source: "Manager");

        // The empty layout intentionally shows no widgets and is never populated.
        if (layout.Name == EmptyLayoutName)
        {
            return;
        }

        // Load plugins
        foreach (uint pluginId in _plugins.Keys)
        {
            InternalPluginData internalPluginData = _plugins[pluginId];

            // Add plugin to layout if it doesn't exist
            if (!layout.Plugins.TryGetValue(pluginId, out PluginSettings? pluginSettings))
            {
                layout.Plugins.Add(pluginId, new PluginSettings() { Metadata = internalPluginData.Metadata, Owner = layout });
                continue;
            }

            pluginSettings.Metadata = internalPluginData.Metadata;
            pluginSettings.Owner = layout;

            if (pluginSettings.Enabled)
            {
                EnsurePluginWindow(screen, layout, internalPluginData, pluginSettings, null);
            }
        }

        // Remove plugins that are not loaded anymore
        List<uint> pluginIdsToRemove = layout.Plugins.Keys.Where(id => !_plugins.ContainsKey(id)).ToList();
        foreach (uint pluginId in pluginIdsToRemove)
        {
            layout.Plugins.Remove(pluginId);
        }

        layout.UpdatePlugins();
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
