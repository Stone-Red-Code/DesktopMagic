using System.Windows;

namespace DesktopMagic.Dialogs;

public partial class InputDialog : Window
{
    public string ResponseText
    {
        get => textBox.Text;
        set => textBox.Text = value;
    }

    public InputDialog(string content, string title = App.AppName)
    {
        InitializeComponent();

        Resources.MergedDictionaries.Add(App.GetLanguageDictionary());

        label.Content = content;
        Title = title;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}