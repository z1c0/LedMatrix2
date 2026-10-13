using System.Net;
using MatrixClient.Models;
using MatrixClient.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatrixClient.Tests.Helpers;

// Wires up real services over fakes, mirroring the registrations in Program.cs
public static class TestServices
{
    public static GalleryService Gallery(string webRoot) => new(new FakeWebHostEnvironment(webRoot));

    // The weather API always fails unless a handler is given, so "weather" renders the no-data image
    public static PixelSenderService Sender(
        FakeHttpHandler pico, string webRoot, TimeProvider? time = null, PicoConfig? config = null, FakeHttpHandler? weatherApi = null,
        PicoStatus? status = null)
    {
        time ??= TimeProvider.System;
        var weather = new WeatherService(
            new WeatherConfig(),
            new FakeHttpClientFactory(weatherApi ?? FakeHttpHandler.Returning(HttpStatusCode.InternalServerError)),
            time,
            NullLogger<WeatherService>.Instance);
        return new PixelSenderService(
            config ?? new PicoConfig(),
            new FakeHttpClientFactory(pico),
            Gallery(webRoot),
            new DynamicImageService(weather, time),
            status ?? Status(),
            NullLogger<PixelSenderService>.Instance);
    }

    public static PicoStatus Status(TimeProvider? time = null) => new(NullLogger<PicoStatus>.Instance, time ?? TimeProvider.System);

    public static AutoSendService AutoSend(AutoSendConfig config, string webRoot, TimeProvider? time = null) =>
        new(config, Gallery(webRoot), time ?? TimeProvider.System);
}
