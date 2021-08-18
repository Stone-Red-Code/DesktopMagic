using System;
using System.Threading;
using System.Windows;

namespace DesktopMagic
{
    /// <summary>
    /// Interaktionslogik für InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog(string content, string title = "InputDialog")
        {
            InitializeComponent();
            label.Content = content;
            this.Title = title;
            SetLanguageDictionary();
        }

        public string ResponseText
        {
            get { return textBox.Text; }
            set { textBox.Text = value; }
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
            ResourceDictionary dict = new ResourceDictionary();
            string currentCulture = Thread.CurrentThread.CurrentCulture.ToString();

            if (currentCulture.Contains("de"))
            {
                dict.Source = new Uri("..\\Resources\\StringResources.de.xaml", UriKind.Relative);
            }
            else
            {
                dict.Source = new Uri("..\\Resources\\StringResources.en.xaml", UriKind.Relative);
            }
            this.Resources.MergedDictionaries.Add(dict);
        }
    }
}