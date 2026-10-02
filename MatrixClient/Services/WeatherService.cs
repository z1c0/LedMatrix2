using System.Globalization;
using System.Text.Json;
using MatrixClient.Models;

namespace MatrixClient.Services;

public class WeatherService(WeatherConfig config, IHttpClientFactory httpClientFactory, TimeProvider time, ILogger<WeatherService> logger)
{
    public record WeatherData(double Temperature, int WeatherCode, string Unit);

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RetryDelay      = TimeSpan.FromMinutes(1);
    // Past this age a reading is more misleading than showing nothing
    private static readonly TimeSpan MaxAge          = TimeSpan.FromHours(2);

    private WeatherData? _cached;
    private DateTimeOffset _fetchedAt;
    private DateTimeOffset _nextFetch = DateTimeOffset.MinValue;

    // Returns null when there's no reasonably fresh reading
    public async Task<WeatherData?> GetCurrentAsync()
    {
        var now = time.GetUtcNow();
        if (now >= _nextFetch)
        {
            try
            {
                var lat  = config.Latitude.ToString(CultureInfo.InvariantCulture);
                var lon  = config.Longitude.ToString(CultureInfo.InvariantCulture);
                var unit = config.TemperatureUnit.ToString().ToLowerInvariant();
                var url  = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                           $"&current=temperature_2m,weather_code&temperature_unit={unit}";

                var client = httpClientFactory.CreateClient();
                var json   = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var curr   = doc.RootElement.GetProperty("current");

                var temp = curr.GetProperty("temperature_2m").GetDouble();
                var code = curr.GetProperty("weather_code").GetInt32();
                var unitLabel = config.TemperatureUnit == TemperatureUnit.Fahrenheit ? "°F" : "°C";

                _cached    = new WeatherData(temp, code, unitLabel);
                _fetchedAt = now;
                _nextFetch = now + RefreshInterval;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch weather from Open-Meteo");
                _nextFetch = now + RetryDelay;
            }
        }

        return _cached is not null && now - _fetchedAt <= MaxAge ? _cached : null;
    }
}
