using DesktopMagic.Plugins;

using System;
using System.Windows;
using System.Windows.Controls;

namespace DesktopMagic.Helpers
{
    internal class SettingElementGenerator
    {
        private readonly ComboBox optionsComboBox;

        public SettingElementGenerator(ComboBox optionsComboBox)
        {
            this.optionsComboBox = optionsComboBox;
        }

        public void Generate(SettingElement settingElement, DockPanel dockPanel, TextBlock textBlock)
        {
            dockPanel.UpdateLayout();
            textBlock.UpdateLayout();
            if (settingElement.Element is DesktopMagicPluginAPI.Inputs.Label eLabel)
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
            }
            else if (settingElement.Element is DesktopMagicPluginAPI.Inputs.Button eButton)
            {
                Button button = new()
                {
                    Content = eButton.Value,
                    FontSize = 10,
                    Height = 20,
                    Margin = new Thickness(0, 10, 0, 10),
                    Padding = new Thickness(0),
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

                _ = dockPanel.Children.Add(button);
            }
            else if (settingElement.Element is DesktopMagicPluginAPI.Inputs.CheckBox eCheckBox)
            {
                CheckBox checkBox = new()
                {
                    IsChecked = eCheckBox.Value,
                    Style = (Style)dockPanel.FindResource("MaterialDesignDarkCheckBox"),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
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

                _ = dockPanel.Children.Add(checkBox);
            }
            else if (settingElement.Element is DesktopMagicPluginAPI.Inputs.TextBox eTextBox)
            {
                TextBox textBox = new()
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
                _ = dockPanel.Children.Add(textBox);
            }
            else if (settingElement.Element is DesktopMagicPluginAPI.Inputs.IntegerUpDown eIntegerUpDown)
            {
                Xceed.Wpf.Toolkit.IntegerUpDown integerUpDown = new()
                {
                    Value = eIntegerUpDown.Value,
                    Minimum = eIntegerUpDown.Minimum,
                    Maximum = eIntegerUpDown.Maximum,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                integerUpDown.ValueChanged += (_s, _e) =>
                {
                    try
                    {
                        eIntegerUpDown.Value = integerUpDown.Value.GetValueOrDefault();
                    }
                    catch (Exception ex)
                    {
                        DisplayException(ex.Message);
                    }
                };
                eIntegerUpDown.OnValueChanged += () =>
                {
                    integerUpDown.Dispatcher.Invoke(() =>
                    {
                        integerUpDown.Value = eIntegerUpDown.Value;
                    });
                };
                _ = dockPanel.Children.Add(integerUpDown);
            }
            else if (settingElement.Element is DesktopMagicPluginAPI.Inputs.Slider eSlider)
            {
                Slider slider = new()
                {
                    Value = eSlider.Value,
                    Minimum = eSlider.Minimum,
                    Maximum = eSlider.Maximum,
                    TickFrequency = 1,
                    IsSnapToTickEnabled = true,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
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

                _ = dockPanel.Children.Add(slider);
            }
            else if (settingElement.Element is DesktopMagicPluginAPI.Inputs.ComboBox eComboBox)
            {
                ComboBox comboBox = new()
                {
                    ItemsSource = eComboBox.Items,
                    IsEditable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    SelectedIndex = 0
                };

                comboBox.SelectionChanged += (_s, _e) =>
                {
                    try
                    {
                        eComboBox.Value = comboBox.Text;
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

                _ = dockPanel.Children.Add(comboBox);
            }
        }

        private void DisplayException(string message)
        {
            App.Logger.Log(message, "PluginInput");
            _ = MessageBox.Show("File execution error:\n" + message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            int index = MainWindow.WindowNames.IndexOf(optionsComboBox.SelectedItem.ToString());

            PluginWindow window = MainWindow.Windows[index];
            window?.Exit();
        }
    }
}