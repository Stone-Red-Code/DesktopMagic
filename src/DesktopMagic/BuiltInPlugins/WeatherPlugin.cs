using DesktopMagic.Api;
using DesktopMagic.Api.Settings;

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopMagic.BuiltInPlugins;

public class WeatherPlugin : AsyncPlugin
{
    [Setting("city-name", "City Name")]
    private readonly TextBox cityInput = new TextBox("Tokyo");

    [Setting("search-btn", "Update Location")]
    private readonly Button searchButton = new Button("Update Location");

    [Setting("show-city", "Show City Name")]
    private readonly CheckBox showCity = new CheckBox(true);

    [Setting("show-temp", "Show Temperature")]
    private readonly CheckBox showTemp = new CheckBox(true);

    [Setting("show-status", "Show Weather Status")]
    private readonly CheckBox showStatus = new CheckBox(true);

    [Setting("show-time", "Show Last Updated")]
    private readonly CheckBox showTime = new CheckBox(false);

    [Setting("font-size", "Base Font Size")]
    private readonly Slider fontSizeSlider = new Slider(10, 100, 30);

    public override int UpdateInterval { get; set; } = 900000; // 15 Minutes

    private readonly HttpClient httpClient = new HttpClient();

    private string? cachedLat;
    private string? cachedLon;
    private string currentTemp = "--";
    private string weatherStatus = "Enter city and search";
    private string foundCityName = "No City Selected";
    private string lastUpdated = "";
    private bool isLoading = false;

    public override async void Start()
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (DesktopMagic)");

        // Setup Event Handlers for instant UI updates when settings change
        showCity.OnValueChanged += Application.UpdateWindow;
        showTemp.OnValueChanged += Application.UpdateWindow;
        showStatus.OnValueChanged += Application.UpdateWindow;
        showTime.OnValueChanged += Application.UpdateWindow;
        fontSizeSlider.OnValueChanged += Application.UpdateWindow;

        await UpdateLocationAndWeather();
        Application.UpdateWindow();

        searchButton.OnClick += () =>
        {
            isLoading = true;
            Application.UpdateWindow();

            _ = Task.Run(async () =>
            {
                await UpdateLocationAndWeather();
                isLoading = false;
                Application.UpdateWindow();
            });
        };
    }

    public override async Task<Bitmap?> MainAsync()
    {
        // Periodic background update
        if (!isLoading && cachedLat != null && cachedLon != null)
        {
            await UpdateWeatherOnly(cachedLat, cachedLon);
        }

        Bitmap bmp = new Bitmap(1200, 800);
        bmp.SetResolution(300, 300);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            int lineCount = 0;
            int baseSize = (int)fontSizeSlider.Value;

            if (isLoading)
            {
                DrawLine(bmp, g, "Fetching...", ref lineCount, baseSize, FontStyle.Italic);
            }
            else
            {
                if (showCity.Value)
                {
                    DrawLine(bmp, g, foundCityName.ToUpper(), ref lineCount, baseSize, FontStyle.Bold);
                }

                if (showTemp.Value)
                {
                    DrawLine(bmp, g, currentTemp, ref lineCount, baseSize, FontStyle.Bold);
                }

                if (showStatus.Value)
                {
                    DrawLine(bmp, g, weatherStatus, ref lineCount, baseSize, FontStyle.Regular);
                }

                if (showTime.Value && !string.IsNullOrEmpty(lastUpdated))
                {
                    DrawLine(bmp, g, $"Updated: {lastUpdated}", ref lineCount, baseSize, FontStyle.Italic);
                }
            }
        }

        return bmp;
    }

    private void DrawLine(Bitmap bitmap, Graphics graphics, string text, ref int lineCount, int fontSize, FontStyle style)
    {
        using Font font = new Font(Application.Theme.Font, fontSize, style);
        SizeF textSize = graphics.MeasureString(text, font);

        float x = (bitmap.Width - textSize.Width) / 2;
        // Adjust vertical spacing based on font size
        float y = (lineCount == 0) ? 60 : (lineCount * (textSize.Height + 10)) + 60;

        using Brush brush = new SolidBrush(Application.Theme.PrimaryColor);
        graphics.DrawString(text, font, brush, x, y);

        lineCount++;
    }

    private async Task UpdateLocationAndWeather()
    {
        try
        {
            string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityInput.Value.Trim())}&count=1&language=en&format=json";
            string geoJson = await httpClient.GetStringAsync(geoUrl);

            using JsonDocument geoDoc = JsonDocument.Parse(geoJson);
            if (!geoDoc.RootElement.TryGetProperty("results", out JsonElement results) || results.GetArrayLength() == 0)
            {
                foundCityName = cityInput.Value;
                currentTemp = "--";
                weatherStatus = "City not found";
                return;
            }

            JsonElement location = results[0];
            cachedLat = location.GetProperty("latitude").GetDouble().ToString(CultureInfo.InvariantCulture);
            cachedLon = location.GetProperty("longitude").GetDouble().ToString(CultureInfo.InvariantCulture);
            foundCityName = location.GetProperty("name").GetString() ?? cityInput.Value;

            // We update the textbox to the "official" name found by the API
            cityInput.Value = foundCityName;

            await UpdateWeatherOnly(cachedLat, cachedLon);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            weatherStatus = "Connection Error";
        }
    }

    private async Task UpdateWeatherOnly(string lat, string lon)
    {
        try
        {
            string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
            string weatherJson = await httpClient.GetStringAsync(weatherUrl);

            using JsonDocument weatherDoc = JsonDocument.Parse(weatherJson);
            JsonElement current = weatherDoc.RootElement.GetProperty("current_weather");

            currentTemp = $"{current.GetProperty("temperature").GetDouble()}°C";
            weatherStatus = MapWeatherCode(current.GetProperty("weathercode").GetInt32());
            lastUpdated = DateTime.Now.ToString("HH:mm");
        }
        catch { /* Ignore background update errors */ }
    }

    private string MapWeatherCode(int code)
    {
        return code switch
        {
            0 => "Clear Sky",
            1 or 2 or 3 => "Partly Cloudy",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rainy",
            71 or 73 or 75 => "Snowy",
            95 or 96 or 99 => "Thunderstorm",
            _ => "Cloudy"
        };
    }
}