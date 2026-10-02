namespace MatrixClient.Models;

public enum TemperatureUnit { Celsius, Fahrenheit }

public class WeatherConfig
{
    public double Latitude { get; set; } = 48.2082;
    public double Longitude { get; set; } = 16.3738;
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;
}
