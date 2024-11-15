using DesktopMagic.Helpers;

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic.Dialogs;

/// <summary>
/// Interaction logic for ColorDialog.xaml
/// </summary>
public partial class ColorDialog : Window
{
    public System.Drawing.Color ResultColor { get; private set; }

    public Brush ResultBrush { get; private set; } = Brushes.Transparent;

    public ColorDialog(string content, System.Drawing.Color defaultColor, string title = "ColorDialog")
    {
        InitializeComponent();
        label.Content = content;
        Title = title;
        SetLanguageDictionary();

        alphaSlider.Value = defaultColor.A;
        redSlider.Value = defaultColor.R;
        greenSlider.Value = defaultColor.G;
        blueSlider.Value = defaultColor.B;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ColorSliders_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        colorHexTextBox.Select(colorHexTextBox.Text.Length, 0);
        SetColorText();
    }

    private void ColorHexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (colorHexTextBox.Text.Length > 0)
        {
            if (colorHexTextBox.Text[0] != '#')
            {
                colorHexTextBox.Text = "#" + colorHexTextBox.Text.Replace("#", "");
            }
        }
        else
        {
            colorHexTextBox.Text = "#";
            colorHexTextBox.Select(colorHexTextBox.Text.Length, 0);
        }

        if (colorHexTextBox.SelectionStart == 0)
        {
            if (colorHexTextBox.Text.Length <= 2)
            {
                colorHexTextBox.Select(colorHexTextBox.Text.Length, 0);
            }
            else
            {
                colorHexTextBox.Select(1, 0);
            }
        }

        string hex = colorHexTextBox.Text;

        if (MultiColorConverter.TryConvertToMediaColor(hex, out Color color) && MultiColorConverter.TryConvertToSystemColor(hex, out System.Drawing.Color systemColor))
        {
            alphaSlider.Value = color.A;
            redSlider.Value = color.R;
            greenSlider.Value = color.G;
            blueSlider.Value = color.B;

            Brush brush = new SolidColorBrush(color);

            ResultBrush = brush;
            ResultColor = systemColor;
            colorRechtangle.Fill = brush;
            colorHexTextBox.Foreground = Brushes.Black;
        }
        else
        {
            colorHexTextBox.Foreground = Brushes.Red;
        }
    }

    private void SetColorText()
    {
        if ((int)alphaSlider.Value == 255)
        {
            colorHexTextBox.Text = "#";
        }
        else
        {
            colorHexTextBox.Text = "#" + ((int)alphaSlider.Value).ToString("X2");
        }

        colorHexTextBox.Text += ((int)redSlider.Value).ToString("X2") + ((int)greenSlider.Value).ToString("X2") + ((int)blueSlider.Value).ToString("X2");
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