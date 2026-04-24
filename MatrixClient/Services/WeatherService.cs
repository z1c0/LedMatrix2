using System.Globalization;
using System.Text.Json;
using MatrixClient.Models;

namespace MatrixClient.Services;

public class WeatherService(WeatherConfig config, IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
{
    public record WeatherData(double Temperature, int WeatherCode, string Unit);

    private WeatherData _cached = new(0, 0, config.TemperatureUnit == "fahrenheit" ? "°F" : "°C");
    private DateTime _cacheExpiry = DateTime.MinValue;

    public async Task<WeatherData> GetCurrentAsync()
    {
        if (DateTime.Now < _cacheExpiry) return _cached;

        try
        {
            var lat  = config.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon  = config.Longitude.ToString(CultureInfo.InvariantCulture);
            var unit = config.TemperatureUnit;
            var url  = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                       $"&current=temperature_2m,weather_code&temperature_unit={unit}";

            var client = httpClientFactory.CreateClient();
            var json   = await client.GetStringAsync(url);
            var doc    = JsonDocument.Parse(json);
            var curr   = doc.RootElement.GetProperty("current");

            var temp = curr.GetProperty("temperature_2m").GetDouble();
            var code = curr.GetProperty("weather_code").GetInt32();
            var unitLabel = unit == "fahrenheit" ? "°F" : "°C";

            _cached      = new WeatherData(temp, code, unitLabel);
            _cacheExpiry = DateTime.Now.AddMinutes(15);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch weather from Open-Meteo");
        }

        return _cached;
    }
}
