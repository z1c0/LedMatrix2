using System.Net;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests;

public class PixelSenderServiceTests : IDisposable
{
    private readonly TempDir _webRoot = new();

    public void Dispose() => _webRoot.Dispose();

    private PixelSenderService CreateSender(FakeHttpHandler pico)
    {
        // The weather service is only needed for the "weather" dynamic mode; fail loudly if it's hit unexpectedly
        var weatherHttp = new FakeHttpClientFactory(FakeHttpHandler.Returning(HttpStatusCode.InternalServerError));
        var weather = new WeatherService(new WeatherConfig(), weatherHttp, NullLogger<WeatherService>.Instance);
        return new PixelSenderService(
            new FakeHttpClientFactory(pico),
            new FakeWebHostEnvironment(_webRoot.Path),
            weather,
            NullLogger<PixelSenderService>.Instance);
    }

    private void SaveImage(string relativePath, int size, Rgba32 fill)
    {
        var full = Path.Combine(_webRoot.Path, "data", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using var img = new Image<Rgba32>(size, size, fill);
        img.SaveAsPng(full);
    }

    // ── ToHex ────────────────────────────────────────────────────────────────

    [Fact]
    public void ToHex_ProducesSixUppercaseHexCharsPerPixel()
    {
        using var img = new Image<Rgba32>(32, 32, new Rgba32(0xAB, 0x0C, 0xEF));

        var hex = PixelSenderService.ToHex(img);

        Assert.Equal(6144, hex.Length);
        Assert.Equal(string.Concat(Enumerable.Repeat("AB0CEF", 1024)), hex);
    }

    [Fact]
    public void ToHex_IsRowMajor()
    {
        using var img = new Image<Rgba32>(32, 32, new Rgba32(0, 0, 0));
        img[1, 0] = new Rgba32(0x11, 0x22, 0x33);   // second pixel of the first row
        img[0, 1] = new Rgba32(0x44, 0x55, 0x66);   // first pixel of the second row

        var hex = PixelSenderService.ToHex(img);

        Assert.Equal("112233", hex.Substring(1 * 6, 6));
        Assert.Equal("445566", hex.Substring(32 * 6, 6));
    }

    [Theory]
    [InlineData(0,   "000000")]
    [InlineData(49,  "000000")]
    [InlineData(50,  "FF8000")]
    [InlineData(255, "FF8000")]
    public void ToHex_TreatsNearlyTransparentPixelsAsBlack(byte alpha, string expected)
    {
        using var img = new Image<Rgba32>(32, 32, new Rgba32(255, 128, 0, alpha));

        Assert.Equal(expected, PixelSenderService.ToHex(img)[..6]);
    }

    // ── SendAsync / SendRawAsync ─────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_PostsGalleryImageAsHex()
    {
        SaveImage("food/apple.png", 32, new Rgba32(255, 0, 0));
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await CreateSender(pico).SendAsync("food/apple.png");

        var (request, body) = Assert.Single(pico.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(string.Concat(Enumerable.Repeat("FF0000", 1024)), body);
    }

    [Fact]
    public async Task SendAsync_RejectsImagesThatAreNot32x32()
    {
        SaveImage("food/big.png", 64, new Rgba32(255, 0, 0));
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        var ex = await Assert.ThrowsAsync<Exception>(() => CreateSender(pico).SendAsync("food/big.png"));

        Assert.Contains("64x64", ex.Message);
        Assert.Empty(pico.Requests);
    }

    [Theory]
    [InlineData("clock")]
    [InlineData("calendar")]
    public async Task SendAsync_RendersDynamicModes(string mode)
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await CreateSender(pico).SendAsync(mode);

        Assert.Equal(6144, Assert.Single(pico.Requests).Body!.Length);
    }

    [Fact]
    public async Task SendAsync_WeatherFallsBackToCachedDataWhenApiFails()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await CreateSender(pico).SendAsync("weather");

        Assert.Equal(6144, Assert.Single(pico.Requests).Body!.Length);
    }

    [Fact]
    public async Task SendRawAsync_ForwardsPayloadVerbatim()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);
        var payload = string.Concat(Enumerable.Repeat("00FF00", 1024));

        await CreateSender(pico).SendRawAsync(payload);

        Assert.Equal(payload, Assert.Single(pico.Requests).Body);
    }

    [Fact]
    public async Task SendRawAsync_ThrowsWithPicoErrorWhenRequestFails()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.BadRequest, "bad length");

        var ex = await Assert.ThrowsAsync<Exception>(() => CreateSender(pico).SendRawAsync("00"));

        Assert.Contains("400", ex.Message);
        Assert.Contains("bad length", ex.Message);
    }
}
