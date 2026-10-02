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

    private static WeatherService Create(FakeHttpHandler handler, WeatherConfig? config = null, TimeProvider? time = null) =>
        new(config ?? new WeatherConfig(), new FakeHttpClientFactory(handler), time ?? TimeProvider.System,
            NullLogger<WeatherService>.Instance);

    // First call succeeds, every later call fails
    private static FakeHttpHandler SucceedsOnce()
    {
        var calls = 0;
        return new FakeHttpHandler(_ => Interlocked.Increment(ref calls) == 1
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ApiResponse) }
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }

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
            var config = new WeatherConfig { Latitude = 48.5167, Longitude = 14.55, TemperatureUnit = TemperatureUnit.Fahrenheit };

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
        var config = new WeatherConfig { TemperatureUnit = TemperatureUnit.Fahrenheit };
        var service = Create(FakeHttpHandler.Returning(HttpStatusCode.OK, ApiResponse), config);

        Assert.Equal("°F", (await service.GetCurrentAsync())?.Unit);
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
    public async Task GetCurrentAsync_ReturnsNullWhenNoDataWasEverFetched()
    {
        var service = Create(FakeHttpHandler.Returning(HttpStatusCode.ServiceUnavailable));

        Assert.Null(await service.GetCurrentAsync());
    }

    [Fact]
    public async Task GetCurrentAsync_BacksOffAfterFailure()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var handler = FakeHttpHandler.Returning(HttpStatusCode.ServiceUnavailable);
        var service = Create(handler, time: time);

        await service.GetCurrentAsync();
        time.Advance(TimeSpan.FromSeconds(30));
        await service.GetCurrentAsync();
        Assert.Single(handler.Requests);

        time.Advance(TimeSpan.FromSeconds(31));
        await service.GetCurrentAsync();
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetCurrentAsync_RefreshesAfterFifteenMinutes()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK, ApiResponse);
        var service = Create(handler, time: time);

        await service.GetCurrentAsync();
        time.Advance(TimeSpan.FromMinutes(14));
        await service.GetCurrentAsync();
        Assert.Single(handler.Requests);

        time.Advance(TimeSpan.FromMinutes(1));
        await service.GetCurrentAsync();
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetCurrentAsync_KeepsLastReadingWhileApiIsBrieflyDown()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var service = Create(SucceedsOnce(), time: time);

        var first = await service.GetCurrentAsync();
        time.Advance(TimeSpan.FromMinutes(30));

        Assert.Equal(first, await service.GetCurrentAsync());
    }

    [Fact]
    public async Task GetCurrentAsync_DropsReadingOnceItIsTooOld()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var service = Create(SucceedsOnce(), time: time);

        Assert.NotNull(await service.GetCurrentAsync());
        time.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(1));

        Assert.Null(await service.GetCurrentAsync());
    }
}
