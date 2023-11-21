using DesktopMagic.Api;
using DesktopMagic.Helpers;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

using Brush = System.Windows.Media.Brush;
using Color = System.Drawing.Color;

namespace DesktopMagic.Plugins;

public class Theme : ITheme, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private Color primaryColor = Color.White;
    private Color secondaryColor = Color.White;
    private Color backgroundColor = Color.Transparent;
    private string font = "Segoe UI";
    private int cornerRadius;
    private int margin;

    public Color PrimaryColor
    {
        get => primaryColor;
        set
        {
            if (primaryColor != value)
            {
                primaryColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PrimaryColorBrush));
            }
        }
    }

    public Color SecondaryColor
    {
        get => secondaryColor;
        set
        {
            if (secondaryColor != value)
            {
                secondaryColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SecondaryColorBrush));
            }
        }
    }

    public Color BackgroundColor
    {
        get => backgroundColor;
        set
        {
            if (backgroundColor != value)
            {
                backgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundColorBrush));
            }
        }
    }

    public string Font
    {
        get => font;
        set
        {
            if (font != value)
            {
                font = value;
                OnPropertyChanged();
            }
        }
    }

    public int CornerRadius
    {
        get => cornerRadius;
        set
        {
            if (cornerRadius != value)
            {
                cornerRadius = value;
                OnPropertyChanged();
            }
        }
    }

    public int Margin
    {
        get => margin;
        set
        {
            if (margin != value)
            {
                margin = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    public Brush PrimaryColorBrush => new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(PrimaryColor));

    [JsonIgnore]
    public Brush SecondaryColorBrush => new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(SecondaryColor));

    [JsonIgnore]
    public Brush BackgroundColorBrush => new SolidColorBrush(MultiColorConverter.ConvertToMediaColor(BackgroundColor));

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}