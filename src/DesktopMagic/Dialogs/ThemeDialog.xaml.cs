using DesktopMagic.Helpers;
using DesktopMagic.Plugins;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopMagic.Dialogs;

/// <summary>
/// Interaction logic for ColorDialog.xaml
/// </summary>
public partial class ThemeDialog : Wpf.Ui.Controls.FluentWindow
{
    private readonly Theme theme;

    public ThemeDialog(string content, Theme theme, string title = App.AppName)
    {
        this.theme = theme;

        InitializeComponent();

        Resources.MergedDictionaries.Add(App.LanguageDictionary);

        cornerRadiusTextBox.Value = theme.CornerRadius;
        marginTextBox.Value = theme.Margin;
        primaryColorRechtangle.Background = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(theme.PrimaryColor));
        secondaryColorRechtangle.Background = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(theme.SecondaryColor));
        backgroundColorRechtangle.Background = new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(theme.BackgroundColor));

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
            primaryColorRechtangle.Background = colorDialog.ResultBrush;
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
            secondaryColorRechtangle.Background = colorDialog.ResultBrush;
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
            backgroundColorRechtangle.Background = colorDialog.ResultBrush;
        }
    }

    private void MarginTextBox_ValueChanged(object sender, Wpf.Ui.Controls.NumberBoxValueChangedEventArgs args)
    {
        if (args.NewValue is >= 0)
        {
            theme.Margin = (int)args.NewValue;
        }
    }

    private void CornerRadiusTextBox_ValueChanged(object sender, Wpf.Ui.Controls.NumberBoxValueChangedEventArgs args)
    {
        if (args.NewValue is >= 0)
        {
            theme.CornerRadius = (int)args.NewValue;
        }
    }
}