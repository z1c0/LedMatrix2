using System.Diagnostics;
using System.Text;
using MatrixClient.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public class PixelSenderService(PicoConfig pico, IHttpClientFactory httpClientFactory, GalleryService gallery, DynamicImageService dynamicImages, PicoStatus status, ILogger<PixelSenderService> logger)
{
    // The Pico's HTTP server handles one request at a time, so manual, draw and auto sends take turns
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // Sends a gallery path ("food/apple.png") or a dynamic mode ("clock")
    public async Task SendAsync(string path)
    {
        using var image = DynamicImageService.IsDynamic(path)
            ? await dynamicImages.RenderAsync(path)
            : await Image.LoadAsync<Rgba32>(gallery.ResolvePath(path));
        if (image.Width != 32 || image.Height != 32)
            throw new InvalidDataException($"Image must be 32x32, got {image.Width}x{image.Height}");

        await SendCoreAsync(new PicoStatus.SentContent(path, ToHex(image)));
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

    // Inverse of ToHex, for showing what was sent; null when the payload isn't a full 32x32 hex image
    public static Image<Rgba32>? FromHex(string hex)
    {
        if (hex.Length != 32 * 32 * 6) return null;
        var image = new Image<Rgba32>(32, 32);
        for (var i = 0; i < 32 * 32; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 6, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) ||
                !byte.TryParse(hex.AsSpan(i * 6 + 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) ||
                !byte.TryParse(hex.AsSpan(i * 6 + 4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                image.Dispose();
                return null;
            }
            image[i % 32, i / 32] = new Rgba32(r, g, b);
        }
        return image;
    }

    public Task SendRawAsync(string hexPayload) => SendCoreAsync(new PicoStatus.SentContent(null, hexPayload));

    private async Task SendCoreAsync(PicoStatus.SentContent sent)
    {
        await _sendLock.WaitAsync();
        try
        {
            using var content = new StringContent(sent.Hex, Encoding.UTF8, "application/text");
            using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(pico.TimeoutSeconds));
            var stopwatch     = Stopwatch.StartNew();

            HttpResponseMessage response;
            try
            {
                response = await httpClientFactory.CreateClient().PostAsync(pico.Url, content, cts.Token);
            }
            catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
            {
                status.ReportAttempt(sent, false, $"No response within {pico.TimeoutSeconds} s", stopwatch.Elapsed);
                status.ReportUnreachable();
                throw new PicoException($"Matrix did not respond within {pico.TimeoutSeconds} s", ex);
            }
            catch (HttpRequestException ex)
            {
                status.ReportAttempt(sent, false, $"Unreachable ({ex.Message})", stopwatch.Elapsed);
                status.ReportUnreachable();
                throw new PicoException($"Matrix is unreachable ({ex.Message})", ex);
            }

            using (response)
            {
                var body   = await response.Content.ReadAsStringAsync();
                var result = $"{(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body.Trim(), 200)}";
                status.ReportAttempt(sent, response.IsSuccessStatusCode, result, stopwatch.Elapsed);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Pico returned {StatusCode}: {Error}", (int)response.StatusCode, body);
                    throw new PicoException($"Matrix returned {(int)response.StatusCode}: {body}");
                }
            }
            status.ReportSent(sent);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
