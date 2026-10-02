using System.Globalization;
using System.Net;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatrixClient.Tests;

public class WeatherServiceTests
{
    private const string ApiResponse = """{"current":{"temperature_2m":21.7,"weather_code":3}}""";

    private static WeatherService Create(FakeHttpHandler handler, WeatherConfig? config = null) =>
        new(config ?? new WeatherConfig(), new FakeHttpClientFactory(handler), NullLogger<WeatherService>.Instance);

    [Fact]
    public async Task GetCurrentAsync_ParsesOpenMeteoResponse()
    {
        var service = Create(FakeHttpHandler.Returning(HttpStatusCode.OK, ApiResponse));

        var data = await service.GetCurrentAsync();

        Assert.Equal(new WeatherService.WeatherData(21.7, 3, "°C"), data);
    }

    [Fact]
    public async Task GetCurrentAsync_BuildsUrlWithInvariantCoordinates()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-AT");   // decimal comma
        try
        {
            var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, ApiResponse);
            var config = new WeatherConfig { Latitude = 48.5167, Longitude = 14.55, TemperatureUnit = "fahrenheit" };

            await Create(handler, config).GetCurrentAsync();

            var url = Assert.Single(handler.Requests).Request.RequestUri!.ToString();
            Assert.Contains("latitude=48.5167", url);
            Assert.Contains("longitude=14.55", url);
            Assert.Contains("temperature_unit=fahrenheit", url);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task GetCurrentAsync_UsesFahrenheitLabel()
    {
        var config = new WeatherConfig { TemperatureUnit = "fahrenheit" };
        var service = Create(FakeHttpHandler.Returning(HttpStatusCode.OK, ApiResponse), config);

        Assert.Equal("°F", (await service.GetCurrentAsync()).Unit);
    }

    [Fact]
    public async Task GetCurrentAsync_CachesResult()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, ApiResponse);
        var service = Create(handler);

        await service.GetCurrentAsync();
        await service.GetCurrentAsync();

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsFallbackWhenApiFails()
    {
        var service = Create(FakeHttpHandler.Returning(HttpStatusCode.ServiceUnavailable));

        var data = await service.GetCurrentAsync();

        Assert.Equal(new WeatherService.WeatherData(0, 0, "°C"), data);
    }
}
