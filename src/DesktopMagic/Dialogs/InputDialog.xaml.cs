using System;
using System.Threading;
using System.Windows;

namespace DesktopMagic.Dialogs;

public partial class InputDialog : Window
{
    public string ResponseText
    {
        get => textBox.Text;
        set => textBox.Text = value;
    }

    public InputDialog(string content, string title = "InputDialog")
    {
        InitializeComponent();
        label.Content = content;
        Title = title;
        SetLanguageDictionary();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SetLanguageDictionary()
    {
        ResourceDictionary dict = [];
        string currentCulture = Thread.CurrentThread.CurrentCulture.ToString();

        if (currentCulture.Contains("de"))
        {
            dict.Source = new Uri("..\\Resources\\Strings\\StringResources.de.xaml", UriKind.Relative);
        }
        else
        {
            dict.Source = new Uri("..\\Resources\\Strings\\StringResources.en.xaml", UriKind.Relative);
        }
        Resources.MergedDictionaries.Add(dict);
    }
}