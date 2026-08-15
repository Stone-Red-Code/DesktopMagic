using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DesktopMagic.Pages;

/// <summary>
/// Interaction logic for MainPage.xaml
/// </summary>
public partial class MainPage : Page
{
    private readonly Manager _manager = Manager.Instance;
    private readonly MainWindowDataContext _dataContext;

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

        _dataContext.RefreshScreens();
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
            _dataContext.Settings = _manager.Settings;
            ApplyPluginsFilter();
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

    private void ScreenSelectorButton_Click(object sender, RoutedEventArgs e)
    {
        ScreenSelectorDialog dialog = new(_dataContext)
        {
            Owner = Window.GetWindow(this)
        };

        _ = dialog.ShowDialog();
    }

    private void PluginCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Control checkBox)
        {
            return;
        }

        uint pluginId = uint.Parse(checkBox.Tag.ToString()!);

        _manager.LoadPlugin(pluginId, _manager.SelectedLayout, (internalPluginData) =>
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

    #region Plugin Search

    private void AllPluginsSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyPluginsFilter();
    }

    private void ApplyPluginsFilter()
    {
        string? searchText = _dataContext.PluginsSearchText;

        System.ComponentModel.ICollectionView view = CollectionViewSource.GetDefaultView(pluginsItemsControl.ItemsSource);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = (item) =>
            {
                if (item is KeyValuePair<uint, PluginSettings> kvp)
                {
                    return kvp.Value.Metadata.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            };
        }
    }

    #endregion

    #region Layout Management

    private void LayoutsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplySelectedLayout();
    }

    /// <summary>
    /// Binds the currently selected layout to the currently selected screen and reloads it.
    /// No-op when the layout is already the one bound to the screen (e.g. programmatic resets).
    /// </summary>
    private void ApplySelectedLayout()
    {
        if (_dataContext.SelectedScreenId is null)
        {
            return;
        }

        System.Windows.Forms.Screen? screen = ScreenUtilities.GetScreenByDeviceName(_dataContext.SelectedScreenId);
        if (screen is null)
        {
            return;
        }

        string? layoutName = _dataContext.SelectedLayoutName;
        if (layoutName is null)
        {
            return;
        }

        Layout? layout = _manager.Settings.Layouts.FirstOrDefault(l => l.Name == layoutName);
        if (layout is null)
        {
            return;
        }

        if (_manager.GetLayoutForScreen(screen) == layout)
        {
            return;
        }

        _manager.BindLayoutToScreen(screen, layout);
        _manager.ReloadScreen(screen);
        _dataContext.RefreshSelection();
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

            _manager.Settings.Layouts.Add(new Layout(inputDialog.ResponseText.Trim()));
            _manager.SaveSettings();

            // Select the new layout so the user can apply it to the current screen
            _dataContext.SelectedLayoutName = inputDialog.ResponseText.Trim();
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

        Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
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

        Layout? layout = _manager.Settings.Layouts.FirstOrDefault(l => l.Name == _dataContext.SelectedLayoutName);
        if (layout is null)
        {
            return;
        }

        if (layout.Name == Manager.EmptyLayoutName)
        {
            Wpf.Ui.Controls.MessageBox cannotDeleteMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = App.AppName,
                Content = (string)FindResource("cannotDeleteEmptyLayout"),
                CloseButtonText = "Ok"
            };
            _ = await cannotDeleteMessageBox.ShowDialogAsync();
            return;
        }

        _ = _manager.Settings.Layouts.Remove(layout);

        // Remove any screen bindings pointing to the deleted layout
        List<string> boundScreens = _manager.Settings.ScreenLayouts
            .Where(kvp => kvp.Value == layout.Name)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string screenId in boundScreens)
        {
            _ = _manager.Settings.ScreenLayouts.Remove(screenId);
        }

        _manager.SaveSettings();
        _manager.LoadLayout();
        _dataContext.RefreshSelection();
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

            FrameworkElement? control = settingElementGenerator.Generate(settingElement, textBlock);

            if (control is not null)
            {
                control.MinWidth = 200;
                control.MaxWidth = 200;
                card.Content = control;
            }
        }

        optionsPanel.UpdateLayout();
    }
}
