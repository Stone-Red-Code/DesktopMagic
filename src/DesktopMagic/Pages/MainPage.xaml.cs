using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DesktopMagic.Pages;

/// <summary>
/// Interaction logic for MainPage.xaml
/// </summary>
public partial class MainPage : Page
{
    private readonly Manager _manager = Manager.Instance;
    private readonly MainWindowDataContext _dataContext;
    private bool _isLoadingLayout = false;

    public MainPage()
    {
        InitializeComponent();

        _dataContext = new MainWindowDataContext
        {
            Settings = _manager.Settings
        };

        DataContext = _dataContext;

        // Subscribe to manager events
        _manager.PluginsChanged += OnPluginsChanged;
        _manager.EditModeChanged += OnEditModeChanged;

        Loaded += MainPage_Loaded;
        Unloaded += MainPage_Unloaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize edit checkbox state
        editCheckBox.IsChecked = _manager.IsEditMode;
    }

    private void MainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe from events
        _manager.PluginsChanged -= OnPluginsChanged;
        _manager.EditModeChanged -= OnEditModeChanged;
    }

    private void OnPluginsChanged()
    {
        Dispatcher.Invoke(() =>
        {
            // Refresh the UI if needed
            _dataContext.Settings = _manager.Settings;
        });
    }

    private void OnEditModeChanged(bool editMode)
    {
        Dispatcher.Invoke(() =>
        {
            editCheckBox.IsChecked = editMode;
        });
    }

    private void EditCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _manager.SetEditMode(editCheckBox.IsChecked == true);
    }

    private void PluginCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Control checkBox)
        {
            return;
        }

        uint pluginId = uint.Parse(checkBox.Tag.ToString()!);

        _manager.LoadPlugin(pluginId, (internalPluginData) =>
        {
            Dispatcher.Invoke(() =>
            {
                // Update the card expander to show options when enabling a plugin
                Wpf.Ui.Controls.CardExpander? cardExpander = ((checkBox.Parent as FrameworkElement)?.Parent) as Wpf.Ui.Controls.CardExpander;

                if (cardExpander is not null)
                {
                    OptionsCardExpander_Expanded(cardExpander, new RoutedEventArgs());
                }
            });
        });
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scv)
        {
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    #region Layout Management

    private void LayoutsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Prevent recursive calls and only process if fully loaded
        if (_isLoadingLayout || !_manager.IsLoaded || !IsLoaded)
        {
            return;
        }

        // Check if this is actually a user-initiated change
        // by verifying that the removed and added items are different
        if (e.RemovedItems.Count > 0 && e.AddedItems.Count > 0)
        {
            if (e.RemovedItems[0] == e.AddedItems[0])
            {
                return;
            }
        }

        try
        {
            _isLoadingLayout = true;
            _manager.SaveSettings();
            _manager.LoadLayout(false);
        }
        finally
        {
            _isLoadingLayout = false;
        }
    }

    private async void NewLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        InputDialog inputDialog = new((string)FindResource("enterLayoutName"))
        {
            Owner = Window.GetWindow(this)
        };

        if (inputDialog.ShowDialog() == true)
        {
            if (_manager.Settings.Layouts.Any(l => l.Name.Trim() == inputDialog.ResponseText.Trim()))
            {
                Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = App.AppName,
                    Content = (string)FindResource("layoutAlreadyExists"),
                    CloseButtonText = "Ok"
                };
                _ = await messageBox.ShowDialogAsync();
                return;
            }

            try
            {
                _isLoadingLayout = true;
                _manager.Settings.Layouts.Add(new Layout(inputDialog.ResponseText.Trim()));
                _manager.Settings.CurrentLayoutName = inputDialog.ResponseText.Trim();
                _manager.SaveSettings();
            }
            finally
            {
                _isLoadingLayout = false;
            }
        }
    }

    private async void RemoveLayoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_manager.Settings.Layouts.Count <= 1)
        {
            Wpf.Ui.Controls.MessageBox cannotDeleteMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = App.AppName,
                Content = (string)FindResource("cannotDeleteLastLayout"),
                CloseButtonText = "Ok"
            };
            _ = await cannotDeleteMessageBox.ShowDialogAsync();
            return;
        }

        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = App.AppName,
            Content = (string)FindResource("confirmDeleteLayout"),
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            IsCloseButtonEnabled = false
        };
        Wpf.Ui.Controls.MessageBoxResult result = await messageBox.ShowDialogAsync();
        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) // Primary is "Yes"
        {
            return;
        }

        try
        {
            _isLoadingLayout = true;
            _ = _manager.Settings.Layouts.Remove(_manager.Settings.CurrentLayout);
            _manager.SaveSettings();
        }
        finally
        {
            _isLoadingLayout = false;
        }
    }

    #endregion

    private void OptionsCardExpander_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.CardExpander expander || expander.Tag is not KeyValuePair<uint, PluginSettings> keyValuePair)
        {
            return;
        }

        PluginSettings? pluginSettings = keyValuePair.Value;
        uint pluginId = keyValuePair.Key;

        StackPanel optionsPanel = new StackPanel
        {
            Visibility = Visibility.Visible
        };

        expander.Content = optionsPanel;
        optionsPanel.UpdateLayout();

        // s.Input being null means the plugins has not been loaded yet but the settings are present in the saved configuration.
        if (pluginSettings is null || pluginSettings.Settings.Count == 0 || pluginSettings.Settings.All(s => s.Input is null))
        {
            _ = optionsPanel.Children.Add(new System.Windows.Controls.TextBlock()
            {
                Text = (string)FindResource(pluginSettings?.Enabled == true ? "noOptions" : "enablePluginToConfigure")
            });

            return;
        }

        SettingElementGenerator settingElementGenerator = new SettingElementGenerator(pluginId);

        foreach (SettingElement settingElement in pluginSettings.Settings)
        {
            Wpf.Ui.Controls.CardControl card = new()
            {
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(5)
            };

            System.Windows.Controls.TextBlock textBlock = new()
            {
                Text = string.IsNullOrWhiteSpace(settingElement.Name) ? string.Empty : settingElement.Name,
                Padding = new Thickness(0, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };

            card.Header = textBlock;

            _ = optionsPanel.Children.Add(card);

            Control? control = settingElementGenerator.Generate(settingElement, textBlock);

            if (control is not null)
            {
                control.MinWidth = 200;
                card.Content = control;
            }
        }

        optionsPanel.UpdateLayout();
    }
}
