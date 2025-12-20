using DesktopMagic.BuiltInPlugins;
using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace DesktopMagic
{
    public partial class MainWindow : Window
    {
        private readonly System.Windows.Forms.NotifyIcon notifyIcon = new();

        private readonly MainWindowDataContext mainWindowDataContext = new();

        private readonly Dictionary<PluginMetadata, Type> builtInPlugins = new()
        {
            {new((string)App.LanguageDictionary["musicVisualizer"], 1), typeof(MusicVisualizerPlugin)},
            {new((string)App.LanguageDictionary["time"],2), typeof(TimePlugin)},
            {new((string)App.LanguageDictionary["date"],3), typeof(DatePlugin)},
            {new((string)App.LanguageDictionary["cpuUsage"], 4), typeof(CpuMonitorPlugin)},
            {new((string)App.LanguageDictionary["weather"], 5), typeof(WeatherPlugin)},
        };

        private bool loaded = false;
        private bool blockWindowsClosing = true;
        public static List<PluginWindow> Windows { get; } = [];
        public static List<string> WindowNames { get; } = [];

        private DesktopMagicSettings Settings
        {
            get => mainWindowDataContext.Settings;
            set => mainWindowDataContext.Settings = value;
        }

        public MainWindow()
        {
            DataContext = mainWindowDataContext;

            try
            {
                Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/DesktopMagic;component/icon.ico")).Stream;
                notifyIcon.MouseClick += NotifyIcon_MouseClick;
                notifyIcon.Visible = true;
                notifyIcon.Text = App.AppName;
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
                {
                    Items =
                    {
                        new System.Windows.Forms.ToolStripMenuItem((string)App.LanguageDictionary["open"], null, (s, e) => RestoreWindow()),
                        new System.Windows.Forms.ToolStripMenuItem((string)App.LanguageDictionary["toggleEditMode"], null, (s, e) => { editCheckBox.IsChecked = !editCheckBox.IsChecked; EditCheckBox_Click(null, null); }),
                        new System.Windows.Forms.ToolStripMenuItem((string)App.LanguageDictionary["pluginManager"], null, (s, e) => PluginManagerButton_Click(null!, null!)),
                        new System.Windows.Forms.ToolStripMenuItem("GitHub", null, (s, e) => GitHubButton_Click(null!, null!)),
                        new System.Windows.Forms.ToolStripMenuItem((string)App.LanguageDictionary["quit"], null, (s, e) => Quit()),
                    }
                };

                InitializeComponent();

                Resources.MergedDictionaries.Add(App.LanguageDictionary);

#if DEBUG
                Title = $"{App.AppName} - Dev {System.Windows.Forms.Application.ProductVersion}";
#else
                Title = $"{App.AppName} - {System.Windows.Forms.Application.ProductVersion}";
#endif
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.ToString(), App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Load

        private readonly Dictionary<uint, InternalPluginData> plugins = [];

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //Write To Log File and Load Elements

                App.Logger.LogInfo("Loading Plugin names", source: "Main");
                LoadPlugins();
                App.Logger.LogInfo("Loading Layout names", source: "Main");
                LoadSettings();
                App.Logger.LogInfo("Loading Layout", source: "Main");
                LoadLayout();

                loaded = true;
                App.Logger.LogInfo("Window Loaded", source: "Main");
            }
            catch (Exception ex)
            {
                App.Logger.LogError(ex.Message, source: "Main");
                _ = MessageBox.Show(ex.ToString(), App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPlugins()
        {
            mainWindowDataContext.IsLoading = true;
            plugins.Clear();

            foreach (var buildInPlugin in builtInPlugins.Keys)
            {
                plugins.Add(buildInPlugin.Id, new(buildInPlugin, string.Empty));
            }

            foreach (string directory in Directory.GetDirectories(App.PluginsPath))
            {
                string? pluginDllPath = Directory.GetFiles(directory, "main.dll").FirstOrDefault();
                string? pluginMetadataPath = Directory.GetFiles(directory, "metadata.json").FirstOrDefault();

                if (pluginDllPath is null)
                {
                    App.Logger.LogError($"Plugin \"{directory}\" has no \"main.dll\"", source: "Main");
                    continue;
                }

                if (pluginMetadataPath is null)
                {
                    App.Logger.LogWarn($"Plugin \"{directory}\" has no \"metadata.json\"", source: "Main");
                    continue;
                }

                PluginMetadata? pluginMetadata = JsonSerializer.Deserialize<PluginMetadata>(File.ReadAllText(pluginMetadataPath));

                if (pluginMetadata is null)
                {
                    App.Logger.LogError($"Plugin \"{directory}\" has no valid \"metadata.json\"", source: "Main");
                    continue;
                }

                if (plugins.ContainsKey(pluginMetadata.Id))
                {
                    App.Logger.LogError($"Plugin \"{directory}\" has the same id as another plugin", source: "Main");
                    continue;
                }

                plugins.Add(pluginMetadata.Id, new(pluginMetadata, directory));
            }
            mainWindowDataContext.IsLoading = false;
        }

        #endregion Load

        #region Windows

        private void EditCheckBox_Click(object? sender, RoutedEventArgs? e)
        {
            foreach (PluginWindow window in Windows)
            {
                window.SetEditMode(editCheckBox.IsChecked == true);
            }

            SaveSettings();
        }

        private void PluginCheckBox_Click(object sender, RoutedEventArgs? e)
        {
            if (sender is not CheckBox checkBox)
            {
                return;
            }

            LoadPlugin(uint.Parse(checkBox.Tag.ToString()!));
        }

        private void LoadPlugin(uint pluginId)
        {
            if (!plugins.TryGetValue(pluginId, out InternalPluginData? internalPluginData))
            {
                return;
            }

            if (!Settings.CurrentLayout.Plugins.TryGetValue(pluginId, out PluginSettings? pluginSettings))
            {
                pluginSettings = new PluginSettings();
                Settings.CurrentLayout.Plugins.Add(pluginId, pluginSettings);
            }

            if (WindowNames.Contains(internalPluginData.Metadata.Id.ToString()) || !pluginSettings.Enabled)
            {
                int index = WindowNames.IndexOf(internalPluginData.Metadata.Id.ToString());

                if (index >= 0)
                {
                    try
                    {
                        blockWindowsClosing = false;
                        Windows[index].Close();
                        blockWindowsClosing = true;
                        Windows.RemoveAt(index);
                        WindowNames.RemoveAt(index);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.LogError(ex.Message, source: "Main");
                    }
                }
                return;
            }

            PluginWindow window;

            if (builtInPlugins.TryGetValue(internalPluginData.Metadata, out Type? pluginType))
            {
                window = new PluginWindow((Api.Plugin)Activator.CreateInstance(pluginType)!, internalPluginData.Metadata, pluginSettings)
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

            Action? onPluginLoaded = null;
            onPluginLoaded = () =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (!optionsComboBox.Items.Contains(internalPluginData.Metadata))
                    {
                        _ = optionsComboBox.Items.Add(internalPluginData.Metadata);
                    }
                    optionsComboBox.SelectedIndex = -1;
                    optionsComboBox.SelectedIndex = optionsComboBox.Items.IndexOf(internalPluginData.Metadata);
                    window.PluginLoaded -= onPluginLoaded;
                });
            };

            window.OnExit += () =>
            {
                Windows.Remove(window);
                WindowNames.Remove(window.Title);
                blockWindowsClosing = false;
                window.Close();
                blockWindowsClosing = true;
                pluginSettings.Enabled = false;
            };
            window.PluginLoaded += onPluginLoaded;

            window.ShowInTaskbar = false;
            window.Show();
            window.SetEditMode(editCheckBox.IsChecked == true);
            window.Closing += DisplayWindow_Closing;
            Windows.Add(window);
            WindowNames.Add(window.Title);
        }

        private void DisplayWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = blockWindowsClosing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();

            if (blockWindowsClosing)
            {
                e.Cancel = true;

                editCheckBox.IsChecked = false;
                EditCheckBox_Click(null, null);
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
                Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Visibility = Visibility.Collapsed;
            UpdateLayout();
            foreach (Window window in Windows)
            {
                window.Hide();
            }
            Environment.Exit(0);
        }

        #endregion Windows

        #region Options

        private void AddThemeButton_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new((string)FindResource("enterThemeName"))
            {
                Owner = this
            };

            if (inputDialog.ShowDialog() == true)
            {
                if (Settings.Themes.Any(l => l.Name.Trim() == inputDialog.ResponseText.Trim()))
                {
                    _ = MessageBox.Show((string)FindResource("themeAlreadyExists"), App.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Settings.Themes.Add(new Theme(inputDialog.ResponseText.Trim()));
                Settings.CurrentLayout.CurrentThemeName = inputDialog.ResponseText.Trim();
                SaveSettings();
            }
        }

        private void DeleteThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Themes.Count <= 1)
            {
                _ = MessageBox.Show((string)FindResource("cannotDeleteLastTheme"), App.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show((string)FindResource("confirmDeleteTheme"), App.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            Settings.Themes.Remove((Theme)themesListBox.SelectedItem);
            SaveSettings();
        }

        private void ChangeThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Theme theme = themesListBox.SelectedItem as Theme ?? Settings.CurrentLayout.Theme;

            ThemeDialog themeDialog = new(theme.Name, theme, App.AppName)
            {
                Owner = this
            };

            themeDialog.ShowDialog();
        }

        private void OptionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionsPanel.Visibility = Visibility.Visible;
            optionsPanel.Children.Clear();
            optionsPanel.UpdateLayout();

            if (optionsComboBox.SelectedItem is null)
            {
                return;
            }

            bool success = Settings.CurrentLayout.Plugins.TryGetValue(((PluginMetadata)optionsComboBox.SelectedItem).Id, out Settings.PluginSettings? pluginSettings);
            if (!success || pluginSettings is null || pluginSettings.Settings.Count == 0)
            {
                _ = optionsPanel.Children.Add(new TextBlock() { Text = (string)FindResource("noOptions") });
                return;
            }

            SettingElementGenerator settingElementGenerator = new SettingElementGenerator(optionsComboBox);

            foreach (SettingElement settingElement in pluginSettings.Settings)
            {
                DockPanel dockPanel = new()
                {
                    LastChildFill = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                _ = optionsPanel.Children.Add(dockPanel);

                TextBlock textBlock = new()
                {
                    Text = string.IsNullOrWhiteSpace(settingElement.Name) ? string.Empty : $"{settingElement.Name}:",
                    Padding = new Thickness(0, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                _ = dockPanel.Children.Add(textBlock);
                settingElementGenerator.Generate(settingElement, dockPanel, textBlock);
            }
        }

        #endregion Options

        internal void RestoreWindow()
        {
            for (int i = 0; i < 10; i++)
            {
                ShowInTaskbar = true;
                Visibility = Visibility.Visible;
                SystemCommands.RestoreWindow(this);
                Topmost = true;
                _ = Activate();
                Topmost = false;
            }
        }

        private void Quit()
        {
            blockWindowsClosing = false;
            Close();
        }

        private void OpenPluginsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", App.PluginsPath);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        #region Layout

        private readonly JsonSerializerOptions jsonSettingsOptions = new()
        {
            Converters =
            {
                new ColorJsonConverter()
            }
        };

        private void LayoutsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                SaveSettings();
                LoadLayout(false);
            }
        }

        private void NewLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new((string)FindResource("enterLayoutName"))
            {
                Owner = this
            };

            if (inputDialog.ShowDialog() == true)
            {
                if (Settings.Layouts.Any(l => l.Name.Trim() == inputDialog.ResponseText.Trim()))
                {
                    _ = MessageBox.Show((string)FindResource("layoutAlreadyExists"), App.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Settings.Layouts.Add(new Layout(inputDialog.ResponseText.Trim()));
                Settings.CurrentLayoutName = inputDialog.ResponseText.Trim();
                SaveSettings();
            }
        }

        private void RemoveLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Layouts.Count <= 1)
            {
                _ = MessageBox.Show((string)FindResource("cannotDeleteLastLayout"), App.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show((string)FindResource("confirmDeleteLayout"), App.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            Settings.Layouts.Remove(Settings.CurrentLayout);
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (!loaded)
            {
                return;
            }

            string json = JsonSerializer.Serialize(Settings, jsonSettingsOptions);
            File.WriteAllText(Path.Combine(App.ApplicationDataPath, "settings.json"), json);
        }

        private void LoadSettings()
        {
            if (!File.Exists(Path.Combine(App.ApplicationDataPath, "settings.json")))
            {
                Settings = new DesktopMagicSettings();

                Settings.Layouts.Add(new Layout((string)FindResource("default")));
                Settings.Themes.Add(new Theme((string)FindResource("default")));

                return;
            }

            string json = File.ReadAllText(Path.Combine(App.ApplicationDataPath, "settings.json"));

            Settings = JsonSerializer.Deserialize<DesktopMagicSettings>(json, jsonSettingsOptions) ?? new DesktopMagicSettings();

            if(Settings.Layouts.Count == 0)
            {
                Settings.Layouts.Add(new Layout((string)FindResource("default")));
            }

            if (Settings.Themes.Count == 0)
            {
                Settings.Themes.Add(new Theme((string)FindResource("default")));
            }
        }

        private void LoadLayout(bool minimize = true)
        {
            mainWindowDataContext.IsLoading = true;
            blockWindowsClosing = false;

            foreach (Window window in Windows)
            {
                window.Close();
            }

            editCheckBox.IsChecked = false;
            EditCheckBox_Click(null, null);
            blockWindowsClosing = true;
            Windows.Clear();
            WindowNames.Clear();
            optionsComboBox.Items.Clear();

            bool showWindow = true;

            // Load plugins
            foreach (uint pluginId in plugins.Keys)
            {
                InternalPluginData internalPluginData = plugins[pluginId];

                // Add plugin to layout if it doesn't exist
                if (!Settings.CurrentLayout.Plugins.TryGetValue(pluginId, out PluginSettings? pluginSettings))
                {
                    Settings.CurrentLayout.Plugins.Add(pluginId, new PluginSettings() { Name = internalPluginData.Metadata.Name });

                    continue;
                }

                pluginSettings.Name = internalPluginData.Metadata.Name;

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
            foreach (uint pluginId in Settings.CurrentLayout.Plugins.Keys)
            {
                if (!plugins.ContainsKey(pluginId))
                {
                    Settings.CurrentLayout.Plugins.Remove(pluginId);
                }
            }

            Settings.CurrentLayout.UpdatePlugins();

            if (!showWindow && minimize)
            {
                Close();
            }
            else
            {
                RestoreWindow();
            }

            mainWindowDataContext.IsLoading = false;
        }

        #endregion Layout

        private void UpdatePluginsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPlugins();
            LoadLayout(false);
        }

        private void NotifyIcon_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                RestoreWindow();
            }
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            string uri = "https://github.com/Stone-Red-Code/DesktopMagic";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = uri
            };
            _ = Process.Start(psi);
        }

        private void PluginManagerButton_Click(object sender, RoutedEventArgs e)
        {
            PluginManager pluginManager = new PluginManager
            {
                Owner = this
            };

            pluginManager.ShowDialog();
            LoadPlugins();
            LoadLayout(Visibility != Visibility.Visible);
        }
    }

    internal class InternalPluginData(PluginMetadata pluginMetadata, string directoryPath)
    {
        public PluginMetadata Metadata { get; set; } = pluginMetadata;
        public string DirectoryPath { get; set; } = directoryPath;
    }
}