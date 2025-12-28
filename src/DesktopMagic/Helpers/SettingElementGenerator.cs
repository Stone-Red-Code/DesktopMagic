using DesktopMagic.Plugins;

using System;
using System.Windows;
using System.Windows.Controls;

namespace DesktopMagic.Helpers;

internal class SettingElementGenerator(uint pluginId)
{
    public Control? Generate(SettingElement settingElement, System.Windows.Controls.TextBlock textBlock)
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