using DesktopMagic.BuiltInWindowElements;
using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using Stone_Red_Utilities.StringExtentions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic
{
    public partial class MainWindow : Window
    {
        private readonly System.Windows.Forms.NotifyIcon notifyIcon = new();

        private readonly MainWindowDataContext mainWindowDataContext = new();

        private readonly Dictionary<PluginMetadata, Type> builtInPlugins = new()
        {
            {new("Music Visualizer", 1), typeof(MusicVisualizerPlugin)},
            {new("Time",2), typeof(TimePlugin)},
            {new("Date",3), typeof(DatePlugin)},
            {new("Cpu Usage", 4), typeof(CpuMonitorPlugin)}
        };

        private bool loaded = false;
        private bool blockWindowsClosing = true;
        public static List<PluginWindow> Windows { get; } = [];
        public static List<string> WindowNames { get; } = [];
        internal static bool EditMode { get; set; } = false;

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
                notifyIcon.Click += TaskbarIcon_TrayLeftClick;
                notifyIcon.Visible = true;
                notifyIcon.Text = App.AppName;
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);

                InitializeComponent();

                SetLanguageDictionary();

#if DEBUG
                Title = $"{App.AppName} - Dev {Assembly.GetExecutingAssembly().GetName().Version}";
#else
                Title = $"{App.AppName} - {Assembly.GetExecutingAssembly().GetName().Version}";
#endif
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.ToString());
            }
        }

        #region Load

        private readonly Dictionary<uint, InternalPluginData> plugins = [];

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(App.ApplicationDataPath + "\\Plugins"))
                {
                    _ = Directory.CreateDirectory(App.ApplicationDataPath + "\\Plugins");
                }
                App.Logger.Log("Created Plugins Folder", "Main");

                //Write To Log File and Load Elements

                App.Logger.Log("Loading Plugin names", "Main");
                LoadPlugins();
                App.Logger.Log("Loading Layout names", "Main");
                LoadSettings();
                App.Logger.Log("Loading Layout", "Main");
                LoadLayout();

                loaded = true;
                App.Logger.Log("Window Loaded", "Main");
            }
            catch (Exception ex)
            {
                App.Logger.Log(ex.Message, "Main", LogSeverity.Error);
                _ = MessageBox.Show(ex.ToString());
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

            string pluginsPath = App.ApplicationDataPath + "\\Plugins";

            foreach (string directory in Directory.GetDirectories(pluginsPath))
            {
                string? pluginDllPath = Directory.GetFiles(directory, "main.dll").FirstOrDefault();
                string? pluginMetadataPath = Directory.GetFiles(directory, "metadata.json").FirstOrDefault();

                if (pluginDllPath is null)
                {
                    App.Logger.Log($"Plugin \"{directory}\" has no \"main.dll\"", "Main", LogSeverity.Error);
                    continue;
                }

                if (pluginMetadataPath is null)
                {
                    App.Logger.Log($"Plugin \"{directory}\" has no \"metadata.json\"", "Main", LogSeverity.Warn);
                    continue;
                }

                PluginMetadata? pluginMetadata = JsonSerializer.Deserialize<PluginMetadata>(File.ReadAllText(pluginMetadataPath));

                if (pluginMetadata is null)
                {
                    App.Logger.Log($"Plugin \"{directory}\" has no valid \"metadata.json\"", "Main", LogSeverity.Error);
                    continue;
                }

                if (plugins.ContainsKey(pluginMetadata.Id))
                {
                    App.Logger.Log($"Plugin \"{directory}\" has the same id as another plugin", "Main", LogSeverity.Error);
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
            EditMode = EditCheckBox.IsChecked == true;
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
                        App.Logger.Log(ex.Message, "Main", LogSeverity.Error);
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
                pluginSettings.Enabled = false;
            };
            window.PluginLoaded += onPluginLoaded;

            window.ShowInTaskbar = false;
            window.Show();
            window.ContentRendered += DisplayWindow_ContentRendered;
            window.Closing += DisplayWindow_Closing;
            Windows.Add(window);
            WindowNames.Add(window.Title);
        }

        private void DisplayWindow_ContentRendered(object? sender, EventArgs e)
        {
            if (sender is not Window window)
            {
                return;
            }

            WindowPos.SendWpfWindowBack(window);
            WindowPos.SendWpfWindowBack(window);
        }

        private void DisplayWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = blockWindowsClosing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult msbRes = MessageBox.Show((string)FindResource("wantToCloseProgram"), App.AppName, MessageBoxButton.YesNo);
            e.Cancel = msbRes != MessageBoxResult.Yes;
            SaveSettings();
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

        private void Window_StateChanged(object? sender, EventArgs? e)
        {
            if (WindowState == WindowState.Minimized)
            {
                EditCheckBox.IsChecked = false;
                EditCheckBox_Click(null, null);
                ShowInTaskbar = false;
                Visibility = Visibility.Collapsed;
                foreach (Window item in Windows)
                {
                    WindowPos.SendWpfWindowBack(item);
                    WindowPos.SendWpfWindowBack(item);
                }
            }
        }

        #endregion Windows

        #region Options

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.CurrentLayout.Theme.Font = fontComboBox.SelectedValue.ToString()!.Replace("System.Windows.Controls.ComboBoxItem: ", "");
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
                    Text = $"{settingElement.Name}:",
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

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            int index = 0;
            foreach (FontFamily ff in Fonts.SystemFontFamilies)
            {
                ComboBoxItem comboBoxItem = new()
                {
                    FontFamily = ff,
                    Content = ff.ToString()
                };
                _ = fontComboBox.Items.Add(comboBoxItem);

                if (ff.ToString() == Settings.CurrentLayout.Theme.Font)
                {
                    fontComboBox.SelectedIndex = index;
                }
                index++;
            }
            if (fontComboBox.SelectedIndex == -1)
            {
                fontComboBox.SelectedIndex = 0;
            }
        }

        private void ChangePrimaryColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog("Set Primary Color", Settings.CurrentLayout.Theme.PrimaryColor);
            if (colorDialog.ShowDialog() == true)
            {
                Settings.CurrentLayout.Theme.PrimaryColor = colorDialog.ResultColor;
                primaryColorRechtangle.Fill = colorDialog.ResultBrush;
            }
        }

        private void ChangeSecondaryColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog("Set Secondary Color", Settings.CurrentLayout.Theme.SecondaryColor);
            if (colorDialog.ShowDialog() == true)
            {
                Settings.CurrentLayout.Theme.SecondaryColor = colorDialog.ResultColor;
                secondaryColorRechtangle.Fill = colorDialog.ResultBrush;
            }
        }

        private void ChangeBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog("Set Background Color", Settings.CurrentLayout.Theme.BackgroundColor);
            if (colorDialog.ShowDialog() == true)
            {
                Settings.CurrentLayout.Theme.BackgroundColor = colorDialog.ResultColor;
                backgroundColorRechtangle.Fill = colorDialog.ResultBrush;
            }
        }

        private void CornerRadiusTextBox_TextChanged(object? sender, TextChangedEventArgs? e)
        {
            bool success = int.TryParse(cornerRadiusTextBox.Text, out int cornerRadius);
            if (success)
            {
                cornerRadiusTextBox.Foreground = Brushes.Black;
                Settings.CurrentLayout.Theme.CornerRadius = cornerRadius;
            }
            else
            {
                cornerRadiusTextBox.Foreground = Brushes.Red;
            }
        }

        private void MarginTextBox_TextChanged(object? sender, TextChangedEventArgs? e)
        {
            bool success = int.TryParse(marginTextBox.Text, out int margin);
            if (success)
            {
                marginTextBox.Foreground = Brushes.Black;
                Settings.CurrentLayout.Theme.Margin = margin;
            }
            else
            {
                marginTextBox.Foreground = Brushes.Red;
            }
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
            InputDialog inputDialog = new((string)FindResource("enterLayoutName"));
            if (inputDialog.ShowDialog() == true)
            {
                if (Settings.Layouts.Any(l => l.Name.Trim() == inputDialog.ResponseText.Trim()))
                {
                    _ = MessageBox.Show((string)FindResource("layoutAlreadyExists"));
                    return;
                }

                Settings.Layouts.Add(new Layout(inputDialog.ResponseText.Trim()));
                Settings.CurrentLayoutName = inputDialog.ResponseText.Trim();
                SaveSettings();
            }
        }

        private void RemoveLayoutButton_Click(object sender, RoutedEventArgs e)
        {
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
                Settings = new DesktopMagicSettings()
                {
                    Layouts =
                    [
                        new Layout((string)FindResource("default"))
                    ]
                };

                return;
            }

            string json = File.ReadAllText(Path.Combine(App.ApplicationDataPath, "settings.json"));

            Settings = JsonSerializer.Deserialize<DesktopMagicSettings>(json, jsonSettingsOptions) ?? new DesktopMagicSettings()
            {
                Layouts =
                [
                    new Layout((string)FindResource("default"))
                ]
            };
        }

        private void LoadLayout(bool minimize = true)
        {
            mainWindowDataContext.IsLoading = true;
            cornerRadiusTextBox.Text = Settings.CurrentLayout.Theme.CornerRadius.ToString();
            marginTextBox.Text = Settings.CurrentLayout.Theme.Margin.ToString();
            blockWindowsClosing = false;

            primaryColorRechtangle.Fill = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(Settings.CurrentLayout.Theme.PrimaryColor));
            secondaryColorRechtangle.Fill = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(Settings.CurrentLayout.Theme.SecondaryColor));
            backgroundColorRechtangle.Fill = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(Settings.CurrentLayout.Theme.BackgroundColor));

            CornerRadiusTextBox_TextChanged(null, null);
            MarginTextBox_TextChanged(null, null);

            foreach (Window window in Windows)
            {
                window.Close();
            }

            EditCheckBox.IsChecked = false;
            EditMode = false;
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
                WindowState = WindowState.Minimized;
                Window_StateChanged(null, null);
            }
            else
            {
                WindowState = WindowState.Normal;
                Window_StateChanged(null, null);
            }
            mainWindowDataContext.IsLoading = false;
        }

        #endregion Layout

        private void UpdatePluginsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPlugins();
            LoadLayout(false);
        }

        private void OpenPluginsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start("explorer.exe", App.ApplicationDataPath + "\\Plugins");
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void TaskbarIcon_TrayLeftClick(object? sender, EventArgs e)
        {
            RestoreWindow();
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
            PluginManager pluginManager = new PluginManager();
            pluginManager.ShowDialog();
            LoadPlugins();
            LoadLayout(false);
        }

        private void SetLanguageDictionary()
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
            Resources.MergedDictionaries.Add(dict);
        }
    }

    internal class InternalPluginData(PluginMetadata pluginMetadata, string directoryPath)
    {
        public PluginMetadata Metadata { get; set; } = pluginMetadata;
        public string DirectoryPath { get; set; } = directoryPath;
    }
}