using System.Text;
using MatrixClient.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public class PixelSenderService(PicoConfig pico, IHttpClientFactory httpClientFactory, IWebHostEnvironment env, WeatherService weatherService, TimeProvider time, ILogger<PixelSenderService> logger)
{
    // The Pico's HTTP server handles one request at a time, so manual, draw and auto sends take turns
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public async Task SendAsync(string relativePath)
    {
        using var image = await ResolveImageAsync(relativePath);
        if (image.Width != 32 || image.Height != 32)
            throw new Exception($"Unexpected image dimensions: {image.Width}x{image.Height}");

        await SendRawAsync(ToHex(image));
    }

    // One RRGGBB hex triple per pixel, row-major; near-transparent pixels become black
    public static string ToHex(Image<Rgba32> image)
    {
        var sb = new StringBuilder(image.Width * image.Height * 6);
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                var p = image[x, y];
                if (p.A < 50)
                    sb.Append("000000");
                else
                {
                    sb.Append(p.R.ToString("X2"));
                    sb.Append(p.G.ToString("X2"));
                    sb.Append(p.B.ToString("X2"));
                }
            }
        return sb.ToString();
    }

    public async Task SendRawAsync(string hexPayload)
    {
        await _sendLock.WaitAsync();
        try
        {
            using var content = new StringContent(hexPayload, Encoding.UTF8, "application/text");
            using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(pico.TimeoutSeconds));

            HttpResponseMessage response;
            try
            {
                response = await httpClientFactory.CreateClient().PostAsync(pico.Url, content, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"Pico at {pico.Url} did not respond within {pico.TimeoutSeconds} s");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    logger.LogWarning("Pico returned {StatusCode}: {Error}", (int)response.StatusCode, error);
                    throw new Exception($"Pico returned {(int)response.StatusCode}: {error}");
                }
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private Task<Image<Rgba32>> ResolveImageAsync(string path) => path switch
    {
        "clock"    => Task.FromResult(ClockRenderer.Render(time.GetLocalNow().DateTime)),
        "calendar" => Task.FromResult(CalendarRenderer.Render(time.GetLocalNow().DateTime)),
        "weather"  => WeatherRenderer.RenderAsync(weatherService),
        _          => Task.FromResult(Image.Load<Rgba32>(ResolveDataPath(path)))
    };

    // Maps a gallery path like "food/apple.png" to disk, refusing anything outside wwwroot/data
    private string ResolveDataPath(string relativePath)
    {
        var dataRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "data")) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(dataRoot, relativePath));

        if (!fullPath.StartsWith(dataRoot, StringComparison.Ordinal))
            throw new ArgumentException($"Invalid image path: {relativePath}");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Image not found: {relativePath}");

        return fullPath;
    }
}
