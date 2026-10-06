using DesktopMagic.DataContexts;

using System.Windows;

namespace DesktopMagic.Dialogs;

/// <summary>
/// Modal picker for selecting a screen from a visual monitor layout.
/// Closes itself once a screen has been picked.
/// </summary>
public partial class ScreenSelectorDialog : Wpf.Ui.Controls.FluentWindow
{
    internal ScreenSelectorDialog(MainWindowDataContext dataContext, string title = App.AppName)
    {
        InitializeComponent();

        Resources.MergedDictionaries.Add(App.LanguageDictionary);

        DataContext = dataContext;
        titleBar.Title = title;
        Title = title;

        // Close the dialog once a screen has been picked.
        screenSelector.SelectionChanged += () => DialogResult = true;
    }
}
