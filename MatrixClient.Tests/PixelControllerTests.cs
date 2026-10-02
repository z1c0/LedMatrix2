using System.Net;
using System.Text.Json;
using MatrixClient.Controllers;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests;

public class PixelControllerTests : IDisposable
{
    private readonly TempDir _webRoot = new();
    private readonly FakeHttpHandler _pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

    public void Dispose() => _webRoot.Dispose();

    private PixelController CreateController()
    {
        var env = new FakeWebHostEnvironment(_webRoot.Path);
        var weather = new WeatherService(new WeatherConfig(), new FakeHttpClientFactory(_pico), TimeProvider.System,
            NullLogger<WeatherService>.Instance);
        var sender = new PixelSenderService(new PicoConfig(), new FakeHttpClientFactory(_pico), env, weather,
            NullLogger<PixelSenderService>.Instance);
        return new PixelController(sender, NullLogger<PixelController>.Instance);
    }

    private static JsonElement Json(IActionResult result) =>
        JsonSerializer.SerializeToElement(Assert.IsType<JsonResult>(result).Value);

    // Model binding has already URL-decoded the form value; decoding again would turn '+' into ' '
    [Theory]
    [InlineData("a+b.png")]
    [InlineData("100%.png")]
    [InlineData("with space.png")]
    public async Task SendToMatrix_UsesFileNameAsBound(string name)
    {
        var full = Path.Combine(_webRoot.Path, "data", "food", name);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using (var img = new Image<Rgba32>(32, 32))
            img.SaveAsPng(full);

        var json = Json(await CreateController().SendToMatrix($"food/{name}"));

        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());
        Assert.Single(_pico.Requests);
    }

    [Fact]
    public async Task SendToMatrix_ReportsErrorForPathOutsideDataFolder()
    {
        var json = Json(await CreateController().SendToMatrix("../../appsettings.json"));

        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Empty(_pico.Requests);
    }
}
