using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region Global settings

        public static string GlobalFont { get; protected set; } = "Segoe UI";
        public static Brush GlobalColor { get; protected set; } = Brushes.White;
        public static System.Drawing.Brush GlobalSystemColor { get; protected set; } = System.Drawing.Brushes.White;
        public static bool EditMode { get; protected set; } = false;

        #endregion Global settings

        #region Music Visualizer Settings

        public static int SpectrumMode { get; protected set; } = 0;
        public static int AmplifierLevel { get; protected set; } = 0;
        public static bool MirrorMode { get; protected set; } = false;
        public static bool LineMode { get; protected set; } = false;
        public static System.Drawing.Brush MusicVisualzerColor { get; protected set; }

        #endregion Music Visualizer Settings

        #region Plugins settings

        public static List<Tuple<string, string[,]>> OptionsList { get; set; } = new List<Tuple<string, string[,]>>();

        #endregion Plugins settings

        #region App Name

        public static string AppName { get; } = "Desktop Magic";

        #endregion App Name

        public static List<Window> Windows { get; protected set; } = new List<Window>();
        public static List<string> WindowNames { get; protected set; } = new List<string>();
        private readonly RegistryKey key;
        private readonly System.Windows.Forms.NotifyIcon notifyIcon = new();

        private readonly string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + AppName;
        private readonly string logFilePath;
        private bool loaded = false;

        private bool blockWindowsClosing = true;

        public MainWindow()
        {
            logFilePath = applicationDataPath + "\\Log.txt";
            key = Registry.CurrentUser.CreateSubKey(@"Software\" + AppName);

            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/DesktopMagic;component/icon.ico")).Stream;
            notifyIcon.Click += TaskbarIcon_TrayLeftClick;
            notifyIcon.Visible = true;
            notifyIcon.Text = AppName;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);

            InitializeComponent();

            SetLanguageDictionary();
            this.Title = AppName;
        }

        #region load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(applicationDataPath))
            {
                Directory.CreateDirectory(applicationDataPath);
            }

            File.Create(logFilePath).Close();
            File.WriteAllText(logFilePath, "");
            File.AppendAllText(logFilePath, "\nCreate ApplicationData Folder");

            File.AppendAllText(logFilePath, "\nCreate Plugins Folder");
            if (!Directory.Exists(applicationDataPath + "\\Plugins"))
            {
                Directory.CreateDirectory(applicationDataPath + "\\Plugins");
            }
            File.AppendAllText(logFilePath, "\nCreate layouts.save");
            if (!File.Exists(applicationDataPath + "\\layouts.save"))
            {
                File.WriteAllText(applicationDataPath + "\\layouts.save", ";" + (string)FindResource("default"));
            }

            optionsComboBox.Items.Add(new Tuple<string, int>((string)FindResource("musicVisualizer"), 0));

            //Write To Log File and Load Elements
            File.AppendAllText(logFilePath, "\nLoading Plugin names");
            LoadPlugins();
            File.AppendAllText(logFilePath, "\nLoading Layout names");
            LoadLayoutNames();
            File.AppendAllText(logFilePath, "\nLoading Layout");
            LoadLayout();
            File.AppendAllText(logFilePath, "\nWindow Loaded");
            loaded = true;
        }

        private void LoadPlugins()
        {
            string PluginsPath = applicationDataPath + "\\Plugins";

            foreach (string fileName in Directory.GetFiles(PluginsPath, "*.cs"))
            {
                string PluginName = fileName[(fileName.LastIndexOf("\\") + 1)..].Replace(fileName[fileName.LastIndexOf(".")..], "");
                try
                {
                    Directory.CreateDirectory(PluginsPath + "\\" + PluginName);
                    File.Move(fileName, $"{PluginsPath}\\{PluginName}\\{PluginName}.cs");
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }

            foreach (string fileName in Directory.GetFiles(PluginsPath, "*.dll"))
            {
                string PluginName = fileName[(fileName.LastIndexOf("\\") + 1)..].Replace(fileName[fileName.LastIndexOf(".")..], "");
                try
                {
                    Directory.CreateDirectory(PluginsPath + "\\" + PluginName);
                    File.Move(fileName, $"{PluginsPath}\\{PluginName}\\{PluginName}.dll");
                }
                catch { }
            }

            foreach (string directory in Directory.GetDirectories(PluginsPath))
            {
                foreach (string fileName in Directory.GetFiles(directory).Where(s => s.EndsWith(".dll") || s.EndsWith(".cs")))
                {
                    string badChars = ",#-<>?!=()*,. ";
                    string PluginName = fileName[(fileName.LastIndexOf("\\") + 1)..].Replace(fileName[fileName.LastIndexOf(".")..], "");
                    string clearPluginName = PluginName;

                    if (PluginName == directory[(directory.LastIndexOf("\\") + 1)..])
                    {
                        foreach (char c in badChars)
                        {
                            clearPluginName = clearPluginName.Replace(c, '_');
                        }

                        CheckBox checkBox = new()
                        {
                            Name = "_PluginCb_" + clearPluginName,
                            Content = PluginName
                        };
                        checkBox.Style = (Style)FindResource("MaterialDesignDarkCheckBox");
                        checkBox.Click += CheckBox_Click;

                        bool exsists = false;
                        foreach (UIElement item in stackPanel.Children)
                        {
                            if (((CheckBox)item).Name == "_PluginCb_" + clearPluginName)
                            {
                                exsists = true;
                                break;
                            }
                        }

                        if (!exsists)
                        {
                            stackPanel.Children.Add(checkBox);
                            OptionsList.Add(new Tuple<string, string[,]>(PluginName, new string[1, 5] { { (string)FindResource("noOptions"), "hed", "", "", "" } }));
                        }
                    }
                }
            }
        }

        #endregion load

        #region Windows

        private void EditCheckBox_Click(object sender, RoutedEventArgs e)
        {
            EditMode = (bool)EditCheckBox.IsChecked;
            SaveLayout();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Window window;
            blockWindowsClosing = false;

            switch (checkBox.Name)
            {
                case "TimeCb": window = new TimeWindow(); break;
                case "DateCb": window = new DateWindow(); break;
                case "CpuUsageCb": window = new CpuUsageWindow(); break;
                case "CalendarCb": window = new CalendarWindow(); break;
                case "MusicVisualizerCb": window = new MusicVisualizerWindow(); break;
                default:
                    if (checkBox.Name.Contains("_PluginCb_"))
                    {
                        window = new PluginWindow(checkBox.Content.ToString())
                        {
                            Title = checkBox.Content.ToString()
                        };
                        break;
                    }
                    else
                    {
                        return;
                    }
            }

            if (!WindowNames.Contains(window.Title))
            {
                Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < OptionsList.Count; i++)
                        {
                            if (OptionsList[i].Item1 == checkBox.Content.ToString())
                            {
                                if (!optionsComboBox.Items.Contains(new Tuple<string, int>(checkBox.Content.ToString(), i)))
                                    optionsComboBox.Items.Add(new Tuple<string, int>(checkBox.Content.ToString(), i));
                            }
                        }

                        window.ShowInTaskbar = false;
                        window.Show();
                        window.Closing += DisplayWindow_Closing;
                        Windows.Add(window);
                        WindowNames.Add(window.Title);
                    });
                });
            }
            else
            {
                int index = WindowNames.IndexOf(window.Title);
                Windows[index].Close();
                Windows.RemoveAt(index);
                WindowNames.RemoveAt(index);
                window.Close();
            }
            key.SetValue(checkBox.Name, checkBox.IsChecked.ToString());
            blockWindowsClosing = true;
            SaveLayout();
        }

        private void DisplayWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = blockWindowsClosing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult msbRes = MessageBox.Show((string)FindResource("wantToCloseProgram"), AppName, MessageBoxButton.YesNo);
            if (msbRes == MessageBoxResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            this.UpdateLayout();
            foreach (Window window in Windows)
            {
                window.Hide();
            }
            //taskbarIcon.Visibility = Visibility.Collapsed;
            Environment.Exit(0);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                EditCheckBox.IsChecked = false;
                EditCheckBox_Click(null, null);
                this.ShowInTaskbar = false;
                this.Visibility = Visibility.Collapsed;
            }
        }

        #endregion Windows

        #region options

        private void AmplifierLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            amplifierLevelLabel.Content = (int)amplifierLevelSlider.Value + 1;
            AmplifierLevel = (int)amplifierLevelSlider.Value;
            key.SetValue("AmplifierLevel", (int)amplifierLevelSlider.Value);
            SaveLayout();
        }

        private void MirrorPlugineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            MirrorMode = (bool)mirrorPlugineCheckBox.IsChecked;
            key.SetValue("MirrorPlugine", mirrorPlugineCheckBox.IsChecked.ToString());
            SaveLayout();
        }

        private void LinePlugineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            LineMode = (bool)linePlugineCheckBox.IsChecked;
            key.SetValue("LinePlugine", linePlugineCheckBox.IsChecked.ToString());
            SaveLayout();
        }

        private void MusicVisualizerColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(musicVisualizerColorTextBox.Text) && musicVisualizerColorTextBox.Text != "Default")
            {
                try
                {
                    if (musicVisualizerColorTextBox.Text.Length > 0)
                    {
                        if (musicVisualizerColorTextBox.Text.ToCharArray()[0] != '#')
                        {
                            musicVisualizerColorTextBox.Text = "#" + musicVisualizerColorTextBox.Text.Replace("#", "");
                        }
                    }
                    else
                    {
                        musicVisualizerColorTextBox.Text = "#";
                        musicVisualizerColorTextBox.Select(musicVisualizerColorTextBox.Text.Length, 0);
                    }

                    if (musicVisualizerColorTextBox.SelectionStart == 0)
                    {
                        if (musicVisualizerColorTextBox.Text.Length <= 2)
                        {
                            musicVisualizerColorTextBox.Select(musicVisualizerColorTextBox.Text.Length, 0);
                        }
                        else
                        {
                            musicVisualizerColorTextBox.Select(1, 0);
                        }
                    }

                    string hex = musicVisualizerColorTextBox.Text;
                    hex = hex.Replace("#", "");

                    System.Drawing.Color systemColor = System.Drawing.Color.FromArgb((byte)int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), (byte)int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber), (byte)int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
                    System.Drawing.Brush systemBrush = new System.Drawing.SolidBrush(systemColor);

                    MusicVisualzerColor = systemBrush;
                    musicVisualizerColorTextBox.Foreground = Brushes.Black;

                    key.SetValue("MusicVisualizerColor", musicVisualizerColorTextBox.Text);
                    SaveLayout();
                }
                catch
                {
                    musicVisualizerColorTextBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                MusicVisualzerColor = null;
                key.SetValue("MusicVisualizerColor", musicVisualizerColorTextBox.Text);
                SaveLayout();
            }
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GlobalFont = fontComboBox.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
            key.SetValue("Font", GlobalFont);
            SaveLayout();
        }

        private void SpectrumPlugineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SpectrumMode = spectrumPlugineComboBox.SelectedIndex;
            key.SetValue("SpectrumPlugine", spectrumPlugineComboBox.SelectedIndex);

            mirrorPlugineCheckBox.IsEnabled = spectrumPlugineComboBox.SelectedIndex != 1;
            SaveLayout();
        }

        private void OptionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (optionsComboBox.SelectedIndex < 1)
            {
                if (musicVisualizerOptionsPanel != null)
                {
                    musicVisualizerOptionsPanel.Visibility = Visibility.Visible;
                    optionsPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                musicVisualizerOptionsPanel.Visibility = Visibility.Collapsed;
                optionsPanel.Visibility = Visibility.Visible;
                optionsPanel.UpdateLayout();

                string[,] options = OptionsList[((Tuple<string, int>)optionsComboBox.SelectedItem).Item2].Item2;

                optionsPanel.Children.Clear();

                //options array index:
                //   0        1       2      3      4
                //"Value", "Type", "Name", "min", "max"

                for (int i = 0; i < options.GetLength(0); i++)
                {
                    StackPanel stackPanel = new()
                    {
                        Orientation = Orientation.Horizontal
                    };
                    optionsPanel.Children.Add(stackPanel);

                    TextBlock label = new()
                    {
                        Text = options[i, 2],
                        Padding = new Thickness(0, 0, 3, 0)
                    };

                    stackPanel.Children.Add(label);
                    stackPanel.UpdateLayout();
                    label.UpdateLayout();

                    if (options[i, 1] == "hed")
                    {
                        label.Text = options[i, 0];
                        label.Margin = new Thickness(0, 5, 3, 0);
                        label.FontWeight = FontWeights.Bold;
                        label.Width = optionsPanel.ActualWidth;
                        label.TextWrapping = TextWrapping.WrapWithOverflow;
                    }
                    else if (options[i, 1] == "bol")
                    {
                        CheckBox checkBox = new();
                        _ = bool.TryParse(options[i, 0], out bool isChecked);
                        checkBox.IsChecked = isChecked;

                        checkBox.Width = optionsPanel.ActualWidth - label.ActualWidth;
                        checkBox.Click += Option_ValueChanged;
                        checkBox.Name = "bol_" + i.ToString();
                        stackPanel.Children.Add(checkBox);
                    }
                    else if (options[i, 1] == "txt")
                    {
                        TextBox textBox = new()
                        {
                            Text = options[i, 0],
                            TextWrapping = TextWrapping.Wrap,
                            Width = optionsPanel.ActualWidth - label.ActualWidth
                        };
                        textBox.TextChanged += Option_ValueChanged;
                        textBox.Name = "txt_" + i.ToString();
                        stackPanel.Children.Add(textBox);
                    }
                    else if (options[i, 1] == "num")
                    {
                        Xceed.Wpf.Toolkit.IntegerUpDown integerUpDown = new();

                        _ = int.TryParse(options[i, 0], out int value);
                        _ = int.TryParse(options[i, 3], out int min);
                        _ = int.TryParse(options[i, 4], out int max);

                        integerUpDown.Value = value;
                        integerUpDown.Minimum = min;
                        integerUpDown.Maximum = max;
                        integerUpDown.Width = optionsPanel.ActualWidth - label.ActualWidth;
                        integerUpDown.ValueChanged += Option_ValueChanged;
                        integerUpDown.Name = "num_" + i.ToString();
                        stackPanel.Children.Add(integerUpDown);
                    }
                    else if (options[i, 1] == "sld")
                    {
                        Slider slider = new();

                        _ = int.TryParse(options[i, 0], out int value);
                        _ = int.TryParse(options[i, 3], out int min);
                        _ = int.TryParse(options[i, 4], out int max);

                        slider.Value = value;
                        slider.Minimum = min;
                        slider.Maximum = max;
                        slider.TickFrequency = 1;
                        slider.IsSnapToTickEnabled = true;
                        slider.Width = optionsPanel.ActualWidth - label.ActualWidth;
                        slider.ValueChanged += Option_ValueChanged;
                        slider.Name = "sld_" + i.ToString();
                        stackPanel.Children.Add(slider);
                    }
                }
            }
        }

        private void Option_ValueChanged(object sender, object e)
        {
            DependencyObject dpobj = (DependencyObject)sender;
            string name = (string)dpobj.GetValue(FrameworkElement.NameProperty);
            string value = "";
            int index = int.Parse(name[(name.LastIndexOf("_") + 1)..]);
            name = name.Replace(name[name.LastIndexOf("_")..], "");

            if (name.Contains("txt"))
            {
                value = ((TextBox)sender).Text;
            }
            else if (name.Contains("num"))
            {
                value = ((Xceed.Wpf.Toolkit.IntegerUpDown)sender).Text;
            }
            else if (name.Contains("sld"))
            {
                value = ((Slider)sender).Value.ToString();
            }
            else if (name.Contains("bol"))
            {
                value = ((CheckBox)sender).IsChecked.ToString();
            }

            OptionsList[((Tuple<string, int>)optionsComboBox.SelectedItem).Item2].Item2[index, 0] = value;

            string PluginName = OptionsList[((Tuple<string, int>)optionsComboBox.SelectedItem).Item2].Item1;
            string[,] options = OptionsList[((Tuple<string, int>)optionsComboBox.SelectedItem).Item2].Item2;
            string optionsText = "";

            try
            {
                int ind = WindowNames.FindIndex(x => x.StartsWith(PluginName));
                ((PluginWindow)Windows[ind]).OptionChanged(index);
            }
            catch { }

            for (int i = 0; i < options.GetLength(0); i++)
            {
                for (int j = 0; j < options.GetLength(1); j++)
                {
                    if (j != 0)
                        optionsText += "\t";
                    optionsText += options[i, j];
                }
                optionsText += "\n";
            }
            File.WriteAllText($"{applicationDataPath}\\Plugins\\{PluginName}\\{PluginName}.save", optionsText);
        }

        #endregion options

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            int index = 0;
            char[] chars = { '1', '0' };
            foreach (FontFamily ff in Fonts.SystemFontFamilies)
            {
                double charWidth = -1;
                bool monospace = true;

                textBlock.FontFamily = ff;

                foreach (char c in chars)
                {
                    textBlock.Text = "";
                    for (int i = 0; i < 100; i++)
                    {
                        textBlock.Text += c.ToString();
                    }
                    textBlock.UpdateLayout();

                    if (charWidth != textBlock.ActualWidth && charWidth != -1)
                    {
                        monospace = false;
                        break;
                    }
                    charWidth = textBlock.ActualWidth;
                }
                if (monospace == true)
                {
                    ComboBoxItem comboBoxItem = new()
                    {
                        FontFamily = ff,
                        Content = ff.ToString()
                    };
                    fontComboBox.Items.Add(comboBoxItem);

                    if (ff.ToString() == GlobalFont)
                    {
                        fontComboBox.SelectedIndex = index;
                    }
                    index++;
                }
            }
            if (fontComboBox.SelectedIndex == -1)
            {
                fontComboBox.SelectedIndex = 0;
            }
        }

        #region color

        private void ColorSliders_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            colorHexTextBox.Text = "#" + ((int)redSlider.Value).ToString("X2") + ((int)greenSlider.Value).ToString("X2") + ((int)blueSlider.Value).ToString("X2");
            colorHexTextBox.Select(colorHexTextBox.Text.Length, 0);
        }

        private void ColorHexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (colorHexTextBox.Text.Length > 0)
                {
                    if (colorHexTextBox.Text.ToCharArray()[0] != '#')
                    {
                        colorHexTextBox.Text = "#" + colorHexTextBox.Text.Replace("#", "");
                    }
                }
                else
                {
                    colorHexTextBox.Text = "#";
                    colorHexTextBox.Select(colorHexTextBox.Text.Length, 0);
                }

                if (colorHexTextBox.SelectionStart == 0)
                {
                    if (colorHexTextBox.Text.Length <= 2)
                    {
                        colorHexTextBox.Select(colorHexTextBox.Text.Length, 0);
                    }
                    else
                    {
                        colorHexTextBox.Select(1, 0);
                    }
                }

                string hex = colorHexTextBox.Text;
                hex = hex.Replace("#", "");
                Color color = Color.FromRgb((byte)int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), (byte)int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber), (byte)int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
                System.Drawing.Color systemColor = System.Drawing.Color.FromArgb((byte)int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), (byte)int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber), (byte)int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));

                redSlider.Value = color.R;
                greenSlider.Value = color.G;
                blueSlider.Value = color.B;

                Brush brush = new SolidColorBrush(color);
                System.Drawing.Brush systemBrush = new System.Drawing.SolidBrush(systemColor);

                GlobalColor = brush;
                GlobalSystemColor = systemBrush;
                colorRechtangle.Fill = brush;
                colorHexTextBox.Foreground = Brushes.Black;

                key.SetValue("Color", colorHexTextBox.Text);
                SaveLayout();
            }
            catch
            {
                colorHexTextBox.Foreground = Brushes.Red;
            }
        }

        #endregion color

        #region Layout

        private void LayoutsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (layoutsComboBox.SelectedIndex == 0)
            {
                removeLayoutButton.IsEnabled = false;
            }
            else
            {
                removeLayoutButton.IsEnabled = true;
            }

            if (layoutsComboBox.SelectedIndex == -1 || !loaded)
                return;

            key.SetValue("SelectedLayout", layoutsComboBox.SelectedIndex);

            string[] lines = File.ReadAllLines(applicationDataPath + "\\layouts.save");
            string[] data = lines[layoutsComboBox.SelectedIndex].Split(';');
            foreach (string dat in data)
            {
                if (dat.Contains(":"))
                {
                    string value = dat[(dat.LastIndexOf(":") + 1)..];
                    string name = dat.Replace(":" + value, "");
                    key.SetValue(name, value);
                }
            }
            LoadLayout(false);
            for (int i = 0; i < fontComboBox.Items.Count; i++)
            {
                ComboBoxItem comboBoxItem = (ComboBoxItem)fontComboBox.Items[i];
                if (comboBoxItem.FontFamily.ToString() == GlobalFont)
                {
                    fontComboBox.SelectedIndex = i;
                }
            }
        }

        private void NewLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new("Layoutnamen eingeben:");
            if (inputDialog.ShowDialog() == true)
            {
                string content = "";
                foreach (string valueName in key.GetValueNames())
                {
                    if (valueName != "SelectedLayout")
                        content += valueName + ":" + key.GetValue(valueName).ToString() + ";";
                }
                content += inputDialog.ResponseText + "\n";

                File.AppendAllText(applicationDataPath + "\\layouts.save", content);
                LoadLayoutNames();
            }
        }

        private void RemoveLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (layoutsComboBox.SelectedIndex == -1)
                return;

            List<string> lines = File.ReadAllLines(applicationDataPath + "\\layouts.save").ToList();
            lines.RemoveAt(layoutsComboBox.SelectedIndex);
            File.WriteAllLines(applicationDataPath + "\\layouts.save", lines);
            LoadLayoutNames();
            layoutsComboBox.SelectedIndex = 0;
        }

        private void SaveLayout()
        {
            Task.Run(() =>
            {
                lock (applicationDataPath)
                {
                    List<string> lines = File.ReadAllLines(applicationDataPath + "\\layouts.save").ToList();
                    string content = "";
                    foreach (string valueName in key.GetValueNames())
                    {
                        if (valueName != "SelectedLayout")
                            content += valueName + ":" + key.GetValue(valueName).ToString() + ";";
                    }
                    Dispatcher.Invoke(() =>
                    {
                        content += layoutsComboBox.SelectedItem.ToString();
                        lines[layoutsComboBox.SelectedIndex] = content;
                    });
                    File.WriteAllLines(applicationDataPath + "\\layouts.save", lines);
                }
            });
        }

        private void LoadLayoutNames()
        {
            layoutsComboBox.Items.Clear();
            string[] lines = File.ReadAllLines(applicationDataPath + "\\layouts.save");

            foreach (string line in lines)
            {
                string name = line[(line.LastIndexOf(";") + 1)..];
                layoutsComboBox.Items.Add(name);
            }
            layoutsComboBox.SelectedIndex = int.Parse(key.GetValue("SelectedLayout", "0").ToString());
        }

        private void LoadLayout(bool minimize = true)
        {
            GlobalFont = key.GetValue("Font", "Segoe UI").ToString();
            spectrumPlugineComboBox.SelectedIndex = int.Parse(key.GetValue("SpectrumPlugine", "0").ToString());
            amplifierLevelSlider.Value = int.Parse(key.GetValue("AmplifierLevel", "0").ToString());
            mirrorPlugineCheckBox.IsChecked = bool.Parse(key.GetValue("MirrorPlugine", "false").ToString());
            linePlugineCheckBox.IsChecked = bool.Parse(key.GetValue("LinePlugine", "false").ToString());
            musicVisualizerColorTextBox.Text = key.GetValue("MusicVisualizerColor", "").ToString();
            colorHexTextBox.Text = key.GetValue("Color", "#FFFFFF").ToString();
            blockWindowsClosing = false;

            MusicVisualizerColorTextBox_TextChanged(null, null);
            MirrorPlugineCheckBox_Click(null, null);
            LinePlugineCheckBox_Click(null, null);
            SpectrumPlugineComboBox_SelectionChanged(null, null);
            AmplifierLevelSlider_ValueChanged(null, null);

            foreach (Window window in Windows)
            {
                window.Close();
            }

            EditCheckBox.IsChecked = false;
            EditMode = false;
            blockWindowsClosing = true;
            Windows.Clear();
            WindowNames.Clear();

            var list = stackPanel.Children.OfType<CheckBox>();
            bool showWindow = true;

            try
            {
                foreach (CheckBox checkBox in list)
                {
                    try
                    {
                        if (key.GetValue(checkBox.Name, "False").ToString() == "True")
                        {
                            checkBox.IsChecked = true;
                            CheckBox_Click(checkBox, null);
                            showWindow = false;
                        }
                        else
                        {
                            checkBox.IsChecked = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(logFilePath, "\n" + ex);
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFilePath, "\n" + ex);
                MessageBox.Show(ex.ToString());
            }

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
            foreach (Window window in Windows)
            {
                if (window.GetType() == typeof(PluginWindow))
                {
                    ((PluginWindow)window).UpdatePluginWindow();
                }
            }
        }

        private void OpenPluginsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", applicationDataPath + "\\Plugins");
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void TaskbarIcon_TrayLeftClick(object sender, EventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                this.ShowInTaskbar = true;
                this.Visibility = Visibility.Visible;
                SystemCommands.RestoreWindow(this);
                this.Topmost = true;
                this.Activate();
                this.Topmost = false;
            }
        }

        private void SetLanguageDictionary()
        {
            ResourceDictionary dict = new();
            string currentCulture = (Thread.CurrentThread.CurrentCulture.ToString());

            if (currentCulture.Contains("de"))
            {
                dict.Source = new Uri("..\\Resources\\StringResources.de.xaml", UriKind.Relative);
            }
            else
            {
                dict.Source = new Uri("..\\Resources\\StringResources.en.xaml", UriKind.Relative);
            }
            this.Resources.MergedDictionaries.Add(dict);
        }
    }
}