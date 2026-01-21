using DesktopMagic.Dialogs;
using DesktopMagic.Plugins;

using Microsoft.Win32;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic.Helpers;

internal class SettingElementGenerator(uint pluginId)
{
    public FrameworkElement? Generate(SettingElement settingElement, System.Windows.Controls.TextBlock textBlock)
    {
        if (settingElement.Input is DesktopMagic.Api.Settings.Label eLabel)
        {
            textBlock.Text = eLabel.Value;
            textBlock.Margin = new Thickness(0, 5, 3, 0);
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
            textBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            if (eLabel.Bold)
            {
                textBlock.FontWeight = FontWeights.Bold;
            }
            eLabel.OnValueChanged += () =>
            {
                textBlock.Dispatcher.Invoke(() =>
                {
                    textBlock.Text = eLabel.Value;
                });
            };
            return null;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.Button eButton)
        {
            Wpf.Ui.Controls.Button button = new()
            {
                Content = eButton.Value,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            button.Click += (_s, _e) =>
            {
                try
                {
                    eButton.Click();
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };
            eButton.OnValueChanged += () =>
            {
                button.Dispatcher.Invoke(() =>
                {
                    button.Content = eButton.Value;
                });
            };

            return button;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.CheckBox eCheckBox)
        {
            Wpf.Ui.Controls.ToggleSwitch checkBox = new()
            {
                IsChecked = eCheckBox.Value,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Height = 30,
                Style = Application.Current.FindResource("ToggleSwitchContentLeftStyle") as Style
            };
            checkBox.Click += (_s, _e) =>
            {
                try
                {
                    eCheckBox.Value = checkBox.IsChecked.GetValueOrDefault();
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };
            eCheckBox.OnValueChanged += () =>
            {
                checkBox.Dispatcher.Invoke(() =>
                {
                    checkBox.IsChecked = eCheckBox.Value;
                });
            };

            return checkBox;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.TextBox eTextBox)
        {
            Wpf.Ui.Controls.TextBox textBox = new()
            {
                Text = eTextBox.Value,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            textBox.TextChanged += (_s, _e) =>
            {
                try
                {
                    eTextBox.Value = textBox.Text;
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };
            eTextBox.OnValueChanged += () =>
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    textBox.Text = eTextBox.Value;
                });
            };
            return textBox;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.IntegerUpDown eIntegerUpDown)
        {
            Wpf.Ui.Controls.NumberBox numberBox = new()
            {
                Value = eIntegerUpDown.Value,
                Minimum = eIntegerUpDown.Minimum,
                Maximum = eIntegerUpDown.Maximum,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            numberBox.ValueChanged += (_s, _e) =>
            {
                try
                {
                    eIntegerUpDown.Value = (int)numberBox.Value;
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };
            eIntegerUpDown.OnValueChanged += () =>
            {
                numberBox.Dispatcher.Invoke(() =>
                {
                    numberBox.Value = eIntegerUpDown.Value;
                });
            };
            return numberBox;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.Slider eSlider)
        {
            Slider slider = new()
            {
                Value = eSlider.Value,
                Minimum = eSlider.Minimum,
                Maximum = eSlider.Maximum,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 5, 0, 5)
            };
            slider.ValueChanged += (_s, _e) =>
            {
                try
                {
                    eSlider.Value = slider.Value;
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };
            eSlider.OnValueChanged += () =>
            {
                slider.Dispatcher.Invoke(() =>
                {
                    slider.Value = eSlider.Value;
                });
            };

            return slider;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.ComboBox eComboBox)
        {
            ComboBox comboBox = new()
            {
                ItemsSource = eComboBox.Items,
                IsEditable = false,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SelectedItem = eComboBox.Value
            };

            comboBox.SelectionChanged += (_s, _e) =>
            {
                try
                {
                    comboBox.SelectedItem ??= eComboBox.Value;
                    eComboBox.Value = comboBox.SelectedItem?.ToString() ?? eComboBox.Value;
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };

            eComboBox.OnValueChanged += () =>
            {
                comboBox.SelectedItem = eComboBox.Value;
            };

            return comboBox;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.ColorPicker eColorPicker)
        {
            Grid panel = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border colorPreview = new()
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(eColorPicker.Value.A, eColorPicker.Value.R, eColorPicker.Value.G, eColorPicker.Value.B)),
                Margin = new Thickness(0, 0, 10, 0),
                BorderBrush = Application.Current.FindResource("ControlElevationBorderBrush") as Brush,
                BorderThickness = new Thickness(1)
            };
            Grid.SetColumn(colorPreview, 0);

            Wpf.Ui.Controls.Button browseButton = new()
            {
                Content = "Select Color",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetColumn(browseButton, 1);

            browseButton.Click += (_s, _e) =>
            {
                try
                {
                    ColorDialog colorDialog = new("Select a color", eColorPicker.Value)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (colorDialog.ShowDialog() == true)
                    {
                        eColorPicker.Value = colorDialog.ResultColor;
                        colorPreview.Background = new SolidColorBrush(Color.FromArgb(eColorPicker.Value.A, eColorPicker.Value.R, eColorPicker.Value.G, eColorPicker.Value.B));
                    }
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };

            eColorPicker.OnValueChanged += () =>
            {
                colorPreview.Dispatcher.Invoke(() =>
                {
                    colorPreview.Background = new SolidColorBrush(Color.FromArgb(eColorPicker.Value.A, eColorPicker.Value.R, eColorPicker.Value.G, eColorPicker.Value.B));
                });
            };

            _ = panel.Children.Add(colorPreview);
            _ = panel.Children.Add(browseButton);

            return panel;
        }
        else if (settingElement.Input is DesktopMagic.Api.Settings.FileSelector eFileSelector)
        {
            Grid grid = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Wpf.Ui.Controls.TextBox pathTextBox = new()
            {
                Text = eFileSelector.Value,
                IsReadOnly = true,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(pathTextBox, 0);

            Wpf.Ui.Controls.Button browseButton = new()
            {
                Content = "Browse",
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(browseButton, 1);

            browseButton.Click += (_s, _e) =>
            {
                try
                {
                    if (eFileSelector.SelectFolder)
                    {
                        OpenFolderDialog folderDialog = new()
                        {
                            Title = eFileSelector.Title,
                            InitialDirectory = string.IsNullOrEmpty(eFileSelector.Value) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : System.IO.Path.GetDirectoryName(eFileSelector.Value)
                        };

                        if (folderDialog.ShowDialog() == true)
                        {
                            eFileSelector.Value = folderDialog.FolderName;
                            pathTextBox.Text = eFileSelector.Value;
                        }
                    }
                    else
                    {
                        OpenFileDialog fileDialog = new()
                        {
                            Title = eFileSelector.Title,
                            Filter = eFileSelector.Filter,
                            InitialDirectory = string.IsNullOrEmpty(eFileSelector.Value) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : System.IO.Path.GetDirectoryName(eFileSelector.Value)
                        };

                        if (fileDialog.ShowDialog() == true)
                        {
                            eFileSelector.Value = fileDialog.FileName;
                            pathTextBox.Text = eFileSelector.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DisplayException(ex.Message);
                }
            };

            eFileSelector.OnValueChanged += () =>
            {
                pathTextBox.Dispatcher.Invoke(() =>
                {
                    pathTextBox.Text = eFileSelector.Value;
                });
            };

            _ = grid.Children.Add(pathTextBox);
            _ = grid.Children.Add(browseButton);

            return grid;
        }

        return null;
    }

    private async void DisplayException(string message)
    {
        App.Logger.LogInfo(message, source: "PluginInput");
        Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Error",
            Content = "File execution error:\n" + message,
            CloseButtonText = "Ok"
        };
        _ = await messageBox.ShowDialogAsync();
        int index = Manager.Instance.PluginWindows.FindIndex(p => p.PluginMetadata.Id == pluginId);

        if (index >= 0 && index < Manager.Instance.PluginWindows.Count)
        {
            IPluginWindow window = Manager.Instance.PluginWindows[index];
            window?.Exit();
        }
    }
}