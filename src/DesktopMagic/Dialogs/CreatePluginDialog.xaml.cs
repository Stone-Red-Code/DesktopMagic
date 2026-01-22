using System.Windows;

namespace DesktopMagic.Dialogs;

public partial class CreatePluginDialog : Wpf.Ui.Controls.FluentWindow
{
    public string ResponseText
    {
        get => textBox.Text;
        set => textBox.Text = value;
    }

    public CreatePluginDialog()
    {
        InitializeComponent();

        Resources.MergedDictionaries.Add(App.LanguageDictionary);
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