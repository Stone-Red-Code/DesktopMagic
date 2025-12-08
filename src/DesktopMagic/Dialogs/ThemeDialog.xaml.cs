using DesktopMagic.Helpers;
using DesktopMagic.Plugins;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic.Dialogs;

/// <summary>
/// Interaction logic for ColorDialog.xaml
/// </summary>
public partial class ThemeDialog : Window
{
    private readonly Theme theme;

    public ThemeDialog(string content, Theme theme, string title = App.AppName)
    {
        this.theme = theme;

        InitializeComponent();

        Resources.MergedDictionaries.Add(App.LanguageDictionary);

        cornerRadiusTextBox.Text = theme.CornerRadius.ToString();
        marginTextBox.Text = theme.Margin.ToString();
        primaryColorRechtangle.Fill = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(theme.PrimaryColor));
        secondaryColorRechtangle.Fill = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(theme.SecondaryColor));
        backgroundColorRechtangle.Fill = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(theme.BackgroundColor));

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

    private void TextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        int index = 0;
        foreach (FontFamily ff in Fonts.SystemFontFamilies)
        {
            ComboBoxItem comboBoxItem = new()
            {
                FontFamily = ff,
                Content = ff.ToString()
            };
            _ = fontComboBox.Items.Add(comboBoxItem);

            if (ff.ToString() == theme.Font)
            {
                fontComboBox.SelectedIndex = index;
            }
            index++;
        }
        if (fontComboBox.SelectedIndex == -1)
        {
            fontComboBox.SelectedIndex = 0;
        }
    }

    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        theme.Font = fontComboBox.SelectedValue.ToString()!.Replace("System.Windows.Controls.ComboBoxItem: ", "");
    }

    private void ChangePrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog("Set Primary Color", theme.PrimaryColor)
        {
            Owner = this
        };

        if (colorDialog.ShowDialog() == true)
        {
            theme.PrimaryColor = colorDialog.ResultColor;
            primaryColorRechtangle.Fill = colorDialog.ResultBrush;
        }
    }

    private void ChangeSecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog("Set Secondary Color", theme.SecondaryColor)
        {
            Owner = this
        };

        if (colorDialog.ShowDialog() == true)
        {
            theme.SecondaryColor = colorDialog.ResultColor;
            secondaryColorRechtangle.Fill = colorDialog.ResultBrush;
        }
    }

    private void ChangeBackgroundColorButton_Click(object sender, RoutedEventArgs e)
    {
        ColorDialog colorDialog = new ColorDialog("Set Background Color", theme.BackgroundColor)
        {
            Owner = this
        };

        if (colorDialog.ShowDialog() == true)
        {
            theme.BackgroundColor = colorDialog.ResultColor;
            backgroundColorRechtangle.Fill = colorDialog.ResultBrush;
        }
    }

    private void CornerRadiusTextBox_TextChanged(object? sender, TextChangedEventArgs? e)
    {
        bool success = int.TryParse(cornerRadiusTextBox.Text, out int cornerRadius);
        if (success)
        {
            cornerRadiusTextBox.Foreground = Brushes.Black;
            theme.CornerRadius = cornerRadius;
        }
        else
        {
            cornerRadiusTextBox.Foreground = Brushes.Red;
        }
    }

    private void MarginTextBox_TextChanged(object? sender, TextChangedEventArgs? e)
    {
        bool success = int.TryParse(marginTextBox.Text, out int margin);
        if (success)
        {
            marginTextBox.Foreground = Brushes.Black;
            theme.Margin = margin;
        }
        else
        {
            marginTextBox.Foreground = Brushes.Red;
        }
    }
}