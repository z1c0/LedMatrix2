using System.Net;
using System.Text.Json;
using MatrixClient.Controllers;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests;

internal static class ResultAssert
{
    // Status code (200 for plain Json results) and the JSON body of an action result
    public static (int Status, JsonElement Body) Unwrap(IActionResult result) => result switch
    {
        JsonResult json     => (json.StatusCode ?? 200, JsonSerializer.SerializeToElement(json.Value)),
        ObjectResult obj    => (obj.StatusCode ?? 200, JsonSerializer.SerializeToElement(obj.Value)),
        _                   => throw new Xunit.Sdk.XunitException($"Unexpected result type {result.GetType().Name}"),
    };
}

public class PixelControllerTests : IDisposable
{
    private readonly TempDir _webRoot = new();
    private FakeHttpHandler _pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

    public void Dispose() => _webRoot.Dispose();

    private PixelController CreateController() =>
        new(TestServices.Sender(_pico, _webRoot.Path), NullLogger<PixelController>.Instance);

    private void SaveFile(string relativePath, byte[]? bytes = null, int size = 32)
    {
        var full = Path.Combine(_webRoot.Path, "data", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        if (bytes is not null)
            File.WriteAllBytes(full, bytes);
        else
            using (var img = new Image<Rgba32>(size, size))
                img.SaveAsPng(full);
    }

    // Model binding has already URL-decoded the form value; decoding again would turn '+' into ' '
    [Theory]
    [InlineData("a+b.png")]
    [InlineData("100%.png")]
    [InlineData("with space.png")]
    public async Task SendToMatrix_UsesFileNameAsBound(string name)
    {
        SaveFile($"food/{name}");

        var (status, body) = ResultAssert.Unwrap(await CreateController().SendToMatrix($"food/{name}"));

        Assert.Equal(200, status);
        Assert.True(body.GetProperty("success").GetBoolean(), body.ToString());
        Assert.Single(_pico.Requests);
    }

    [Theory]
    [InlineData("../../appsettings.json", StatusCodes.Status400BadRequest, "Invalid image path: ../../appsettings.json")]
    [InlineData("food/nope.png",          StatusCodes.Status404NotFound,   "Image not found: food/nope.png")]
    public async Task SendToMatrix_ReportsBadPaths(string path, int expectedStatus, string expectedError)
    {
        var (status, body) = ResultAssert.Unwrap(await CreateController().SendToMatrix(path));

        Assert.Equal(expectedStatus, status);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal(expectedError, body.GetProperty("error").GetString());
        Assert.Empty(_pico.Requests);
    }

    [Fact]
    public async Task SendToMatrix_ReportsWrongSize()
    {
        SaveFile("food/big.png", size: 64);

        var (status, body) = ResultAssert.Unwrap(await CreateController().SendToMatrix("food/big.png"));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, status);
        Assert.Equal("Image must be 32x32, got 64x64", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SendToMatrix_ReportsCorruptImageWithoutInternals()
    {
        SaveFile("food/broken.png", "not a png"u8.ToArray());

        var (status, body) = ResultAssert.Unwrap(await CreateController().SendToMatrix("food/broken.png"));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, status);
        Assert.Equal("Not a supported image file", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SendToMatrix_ReportsPicoFailureAsBadGateway()
    {
        SaveFile("food/apple.png");
        _pico = FakeHttpHandler.Returning(HttpStatusCode.InternalServerError, "oops");

        var (status, body) = ResultAssert.Unwrap(await CreateController().SendToMatrix("food/apple.png"));

        Assert.Equal(StatusCodes.Status502BadGateway, status);
        Assert.Equal("Matrix returned 500: oops", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SendToMatrix_HidesUnexpectedErrors()
    {
        SaveFile("food/apple.png");
        _pico = new FakeHttpHandler(_ => throw new InvalidOperationException("secret internals"));

        var (status, body) = ResultAssert.Unwrap(await CreateController().SendToMatrix("food/apple.png"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.DoesNotContain("secret", body.GetProperty("error").GetString());
    }
}

public class DrawControllerTests
{
    private readonly FakeHttpHandler _pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

    private DrawController CreateController() =>
        new(TestServices.Sender(_pico, Path.GetTempPath()), NullLogger<DrawController>.Instance);

    [Fact]
    public async Task Send_ForwardsValidHex()
    {
        var pixels = string.Concat(Enumerable.Repeat("00ff00", 1024));

        var (status, _) = ResultAssert.Unwrap(await CreateController().Send(pixels));

        Assert.Equal(200, status);
        Assert.Equal(pixels, Assert.Single(_pico.Requests).Body);
    }

    [Theory]
    [InlineData(null, "got 0 chars")]
    [InlineData("", "got 0 chars")]
    [InlineData("00FF00", "got 6 chars")]
    public async Task Send_RejectsWrongLength(string? pixels, string expected)
    {
        var (status, body) = ResultAssert.Unwrap(await CreateController().Send(pixels));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Contains(expected, body.GetProperty("error").GetString());
        Assert.Empty(_pico.Requests);
    }

    [Fact]
    public async Task Send_RejectsNonHexCharacters()
    {
        var pixels = string.Concat(Enumerable.Repeat("00FF00", 1023)) + "#00FF0";

        var (status, body) = ResultAssert.Unwrap(await CreateController().Send(pixels));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Contains("non-hex", body.GetProperty("error").GetString());
        Assert.Empty(_pico.Requests);
    }
}

public class AutoSendControllerTests : IDisposable
{
    private readonly TempDir _webRoot = new();

    public void Dispose() => _webRoot.Dispose();

    // 2026-10-02 10:30 local (UTC clock, UTC zone)
    private AutoSendController Create(params AutoSendRule[] rules) =>
        new(TestServices.AutoSend(
            new AutoSendConfig { IntervalMinutes = 10, Rules = [.. rules] },
            _webRoot.Path,
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 2, 10, 30, 0, TimeSpan.Zero))));

    [Fact]
    public void Status_ReportsActiveRuleInterval()
    {
        var rule = new AutoSendRule { Name = "Clock", Days = ["Friday"], From = "10:00", To = "11:00", IntervalMinutes = 1 };

        var (_, body) = ResultAssert.Unwrap(Create(rule).Status());

        Assert.Equal("Clock", body.GetProperty("activeRule").GetString());
        Assert.Equal(1, body.GetProperty("intervalMinutes").GetInt32());
    }

    [Fact]
    public void Status_FallsBackToGlobalIntervalWithoutActiveRule()
    {
        var (_, body) = ResultAssert.Unwrap(Create().Status());

        Assert.Equal(JsonValueKind.Null, body.GetProperty("activeRule").ValueKind);
        Assert.Equal(10, body.GetProperty("intervalMinutes").GetInt32());
    }
}

public class DynamicControllerTests
{
    private static DynamicController Create()
    {
        var weather = new WeatherService(new WeatherConfig(),
            new FakeHttpClientFactory(FakeHttpHandler.Returning(HttpStatusCode.InternalServerError)),
            TimeProvider.System, NullLogger<WeatherService>.Instance);
        return new DynamicController(new DynamicImageService(weather, TimeProvider.System));
    }

    [Theory]
    [InlineData("clock")]
    [InlineData("calendar")]
    [InlineData("weather")]
    public async Task Preview_RendersPng(string mode)
    {
        var file = Assert.IsType<FileContentResult>(await Create().Preview(mode));

        Assert.Equal("image/png", file.ContentType);
        using var img = Image.Load<Rgba32>(file.FileContents);
        Assert.Equal((32, 32), (img.Width, img.Height));
    }

    [Fact]
    public async Task Preview_UnknownModeIsNotFound() =>
        Assert.IsType<NotFoundResult>(await Create().Preview("nope"));

    [Fact]
    public void Preview_IsNeverCached()
    {
        var attr = typeof(DynamicController).GetMethod(nameof(DynamicController.Preview))!
            .GetCustomAttributes(typeof(ResponseCacheAttribute), false)
            .Cast<ResponseCacheAttribute>()
            .Single();

        Assert.True(attr.NoStore);
    }
}
