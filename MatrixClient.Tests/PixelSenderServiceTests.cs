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

    private PixelSenderService CreateSender(FakeHttpHandler pico, PicoConfig? config = null) =>
        TestServices.Sender(pico, _webRoot.Path, config: config);

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
    public void FromHex_IsTheInverseOfToHex()
    {
        using var img = new Image<Rgba32>(32, 32, new Rgba32(0, 0, 0));
        img[1, 0] = new Rgba32(0x11, 0x22, 0x33);
        img[0, 1] = new Rgba32(0x44, 0x55, 0x66);
        var hex = PixelSenderService.ToHex(img);

        using var decoded = PixelSenderService.FromHex(hex);

        Assert.NotNull(decoded);
        Assert.Equal(hex, PixelSenderService.ToHex(decoded));
    }

    [Theory]
    [InlineData("00")]
    [InlineData("ZZ")]
    public void FromHex_RejectsIncompleteOrInvalidPayloads(string start)
    {
        var hex = start.Length == 2 && start != "ZZ" ? start : start + new string('0', 6144 - start.Length);

        Assert.Null(PixelSenderService.FromHex(hex));
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

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => CreateSender(pico).SendAsync("food/big.png"));

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
    public async Task SendAsync_WeatherStillSendsWhenApiFails()
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

        var ex = await Assert.ThrowsAsync<PicoException>(() => CreateSender(pico).SendRawAsync("00"));

        Assert.Contains("400", ex.Message);
        Assert.Contains("bad length", ex.Message);
    }

    [Fact]
    public async Task SendAsync_AcceptsPathsThatStayInsideDataFolder()
    {
        SaveImage("food/apple.png", 32, new Rgba32(255, 0, 0));
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await CreateSender(pico).SendAsync("retro/../food/apple.png");

        Assert.Single(pico.Requests);
    }

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("food/../../outside.png")]
    [InlineData("../data-evil/outside.png")]   // shares the "data" prefix but is a sibling folder
    public async Task SendAsync_RejectsPathsOutsideDataFolder(string path)
    {
        // Valid images exist at the target locations, so only the path check can stop them
        SaveImage("../outside.png", 32, new Rgba32(255, 0, 0));
        SaveImage("../data-evil/outside.png", 32, new Rgba32(255, 0, 0));
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateSender(pico).SendAsync(path));
        Assert.Empty(pico.Requests);
    }

    [Fact]
    public async Task SendAsync_RejectsAbsolutePaths()
    {
        SaveImage("../outside.png", 32, new Rgba32(255, 0, 0));
        var absolute = Path.Combine(_webRoot.Path, "outside.png");
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateSender(pico).SendAsync(absolute));
        Assert.Empty(pico.Requests);
    }

    [Fact]
    public async Task SendAsync_ReportsMissingImageWithoutFullPath()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => CreateSender(pico).SendAsync("food/nope.png"));

        Assert.Equal("Image not found: food/nope.png", ex.Message);
    }

    [Fact]
    public async Task SendRawAsync_PostsToConfiguredUrl()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

        await CreateSender(pico, new PicoConfig { Url = "http://10.0.0.7/" }).SendRawAsync("00");

        Assert.Equal(new Uri("http://10.0.0.7/"), Assert.Single(pico.Requests).Request.RequestUri);
    }

    [Fact]
    public async Task SendRawAsync_TimesOutWhenPicoDoesNotAnswer()
    {
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(pico, new PicoConfig { TimeoutSeconds = 0.1 });

        var ex = await Assert.ThrowsAsync<PicoException>(() => sender.SendRawAsync("00"));

        Assert.Contains("did not respond", ex.Message);
    }

    [Fact]
    public async Task SendRawAsync_ReleasesLockAfterTimeout()
    {
        var calls = 0;
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
                await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(pico, new PicoConfig { TimeoutSeconds = 0.1 });

        await Assert.ThrowsAsync<PicoException>(() => sender.SendRawAsync("00"));
        await sender.SendRawAsync("00");

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SendRawAsync_NeverSendsConcurrently()
    {
        int inFlight = 0, maxInFlight = 0;
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            var now = Interlocked.Increment(ref inFlight);
            InterlockedMax(ref maxInFlight, now);
            await Task.Delay(20, ct);
            Interlocked.Decrement(ref inFlight);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(pico);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(() => sender.SendRawAsync("00"))));

        Assert.Equal(5, pico.Requests.Count);
        Assert.Equal(1, maxInFlight);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref target)) &&
               Interlocked.CompareExchange(ref target, value, current) != current) { }
    }
}
