using DesktopMagic.BuiltInWindowElements;
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

        private readonly Dictionary<string, Type> builtInPlugins = new()
        {
            {"Music Visualizer", typeof(MusicVisualizerPlugin)},
            {"Time", typeof(TimePlugin)},
            {"Date", typeof(DatePlugin)},
            {"Cpu Usage", typeof(CpuMonitorPlugin)}
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

        private readonly List<string> pluginNames = [];

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
            pluginNames.Clear();

            pluginNames.AddRange(builtInPlugins.Keys);

            string pluginsPath = App.ApplicationDataPath + "\\Plugins";

            foreach (string fileName in Directory.GetFiles(pluginsPath, "*.dll"))
            {
                string pluginName = fileName[(fileName.LastIndexOf('\\') + 1)..].Replace(fileName[fileName.LastIndexOf('.')..], "");
                try
                {
                    _ = Directory.CreateDirectory(Path.Combine(pluginsPath, pluginName));
                    File.Move(fileName, $"{pluginsPath}\\{pluginName}\\{pluginName}.dll");
                }
                catch (Exception ex)
                {
                    App.Logger.Log(ex.Message, "Main", LogSeverity.Error);
                }
            }

            foreach (string directory in Directory.GetDirectories(pluginsPath))
            {
                foreach (string fileName in Directory.GetFiles(directory).Where(s => s.EndsWith(".dll", StringComparison.InvariantCulture)))
                {
                    string badChars = ",#-<>?!=()*,. ";
                    string pluginName = fileName[(fileName.LastIndexOf('\\') + 1)..].Replace(fileName[fileName.LastIndexOf('.')..], "");
                    string clearPluginName = pluginName;

                    if (pluginName == directory[(directory.LastIndexOf('\\') + 1)..])
                    {
                        foreach (char c in badChars)
                        {
                            clearPluginName = clearPluginName.Replace(c, '_');
                        }

                        pluginNames.Add(pluginName);
                    }
                }
            }
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

            LoadPlugin(checkBox.Content.ToString() ?? string.Empty);
        }

        private void LoadPlugin(string pluginName)
        {
            if (!Settings.CurrentLayout.Plugins.TryGetValue(pluginName, out PluginSettings? pluginSettings))
            {
                pluginSettings = new PluginSettings();
                Settings.CurrentLayout.Plugins.Add(pluginName, pluginSettings);
            }

            PluginWindow window;

            if (builtInPlugins.TryGetValue(pluginName, out Type? pluginType))
            {
                window = new PluginWindow((DesktopMagic.Api.Plugin)Activator.CreateInstance(pluginType)!, pluginName, pluginSettings)
                {
                    Title = pluginName
                };
            }
            else
            {
                window = new PluginWindow(pluginName, pluginSettings)
                {
                    Title = pluginName
                };
            }

            blockWindowsClosing = false;

            if (!WindowNames.Contains(window.Title) && pluginSettings.Enabled)
            {
                _ = Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Action? onPluginLoaded = null;
                        onPluginLoaded = () =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (!optionsComboBox.Items.Contains(pluginName))
                                {
                                    _ = optionsComboBox.Items.Add(pluginName);
                                }
                                optionsComboBox.SelectedIndex = -1;
                                optionsComboBox.SelectedIndex = optionsComboBox.Items.IndexOf(pluginName);
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
                    });
                });
            }
            else
            {
                int index = WindowNames.IndexOf(window.Title);

                if (index >= 0)
                {
                    //Not sure how to handle this
                    try
                    {
                        Windows[index].Close();

                        Windows.RemoveAt(index);
                        WindowNames.RemoveAt(index);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Log(ex.Message, "Main", LogSeverity.Error);
                    }
                }
            }

            blockWindowsClosing = true;
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

            bool success = Settings.CurrentLayout.Plugins.TryGetValue(optionsComboBox.SelectedItem.ToString()!, out Settings.PluginSettings? pluginSettings);
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
            bool sucess = int.TryParse(cornerRadiusTextBox.Text, out int cornerRadius);
            if (sucess)
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
            bool sucess = int.TryParse(marginTextBox.Text, out int margin);
            if (sucess)
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
            foreach (string pluginName in pluginNames)
            {
                // Add plugin to layout if it doesn't exist
                if (!Settings.CurrentLayout.Plugins.TryGetValue(pluginName, out PluginSettings? pluginSettings))
                {
                    Settings.CurrentLayout.Plugins.Add(pluginName, new PluginSettings());

                    continue;
                }

                if (pluginSettings.Enabled)
                {
                    LoadPlugin(pluginName);
                }

                if (showWindow && pluginSettings.Enabled)
                {
                    showWindow = false;
                }
            }

            // Remove plugins that are not loaded anymore
            foreach (string pluginName in Settings.CurrentLayout.Plugins.Keys)
            {
                if (!pluginNames.Contains(pluginName))
                {
                    Settings.CurrentLayout.Plugins.Remove(pluginName);
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

        private void GithubButton_Click(object sender, RoutedEventArgs e)
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
        }

        private void SetLanguageDictionary()
        {
            ResourceDictionary dict = [];
            string currentCulture = Thread.CurrentThread.CurrentUICulture.ToString();

            if (currentCulture.Contains("de"))
            {
                dict.Source = new Uri("..\\Resources\\StringResources.de.xaml", UriKind.Relative);
            }
            else
            {
                dict.Source = new Uri("..\\Resources\\StringResources.en.xaml", UriKind.Relative);
            }
            Resources.MergedDictionaries.Add(dict);
        }
    }
}