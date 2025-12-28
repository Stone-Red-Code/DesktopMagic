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

    private void AddThemeButton_Click(object sender, RoutedEventArgs e)
    {
        InputDialog inputDialog = new((string)FindResource("enterThemeName"))
        {
            Owner = Window.GetWindow(this)
        };

        if (inputDialog.ShowDialog() == true)
        {
            if (_manager.Settings.Themes.Any(l => l.Name.Trim() == inputDialog.ResponseText.Trim()))
            {
                _ = System.Windows.MessageBox.Show((string)FindResource("themeAlreadyExists"), App.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _manager.Settings.Themes.Add(new Theme(inputDialog.ResponseText.Trim()));
            _manager.Settings.CurrentLayout.CurrentThemeName = inputDialog.ResponseText.Trim();
            _manager.SaveSettings();
        }
    }

    private void DeleteThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_manager.Settings.Themes.Count <= 1)
        {
            _ = System.Windows.MessageBox.Show((string)FindResource("cannotDeleteLastTheme"), App.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBoxResult result = System.Windows.MessageBox.Show((string)FindResource("confirmDeleteTheme"), App.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
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
        if (sender is not Button button || button.Tag is not Theme theme)
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
