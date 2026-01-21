using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Plugins;

using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DesktopMagic.Pages;

/// <summary>
/// Interaction logic for ThemePage.xaml
/// </summary>
public partial class ThemePage : Page
{
    private readonly Manager _manager = Manager.Instance;
    private readonly MainWindowDataContext _dataContext;

    public ThemePage()
    {
        InitializeComponent();

        _dataContext = new MainWindowDataContext
        {
            Settings = _manager.Settings
        };

        DataContext = _dataContext;

        // Subscribe to manager events
        _manager.SettingsChanged += OnSettingsChanged;

        Unloaded += ThemePage_Unloaded;
    }

    private void ThemePage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe from events
        _manager.SettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        Dispatcher.Invoke(() =>
        {
            _dataContext.Settings = _manager.Settings;
        });
    }

    private async void AddThemeButton_Click(object sender, RoutedEventArgs e)
    {
        InputDialog inputDialog = new((string)FindResource("enterThemeName"))
        {
            Owner = Window.GetWindow(this)
        };

        if (inputDialog.ShowDialog() == true)
        {
            if (_manager.Settings.Themes.Any(l => l.Name.Trim() == inputDialog.ResponseText.Trim()))
            {
                Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = App.AppName,
                    Content = (string)FindResource("themeAlreadyExists"),
                    CloseButtonText = "Ok"
                };
                _ = await messageBox.ShowDialogAsync();
                return;
            }

            _manager.Settings.Themes.Add(new Theme(inputDialog.ResponseText.Trim()));
            _manager.Settings.CurrentLayout.CurrentThemeName = inputDialog.ResponseText.Trim();
            _manager.SaveSettings();
        }
    }

    private async void DeleteThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_manager.Settings.Themes.Count <= 1)
        {
            Wpf.Ui.Controls.MessageBox cannotDeleteMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = App.AppName,
                Content = (string)FindResource("cannotDeleteLastTheme"),
                CloseButtonText = "Ok"
            };
            _ = await cannotDeleteMessageBox.ShowDialogAsync();
            return;
        }

        var messageBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = App.AppName,
            Content = (string)FindResource("confirmDeleteTheme"),
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            IsCloseButtonEnabled = false
        };
        Wpf.Ui.Controls.MessageBoxResult result = await messageBox.ShowDialogAsync();
        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) // Primary is "Yes"
        {
            return;
        }

        if (themesListBox.SelectedItem is Theme theme)
        {
            _ = _manager.Settings.Themes.Remove(theme);
            _manager.SaveSettings();
        }
    }

    private void EditThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not Theme theme)
        {
            return;
        }

        ThemeDialog themeDialog = new(theme.Name, theme, App.AppName)
        {
            Owner = Window.GetWindow(this)
        };

        _ = themeDialog.ShowDialog();
    }
}
