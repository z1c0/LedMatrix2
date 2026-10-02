using System.Text;
using MatrixClient.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public class PixelSenderService(PicoConfig pico, IHttpClientFactory httpClientFactory, GalleryService gallery, DynamicImageService dynamicImages, ILogger<PixelSenderService> logger)
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
            catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
            {
                throw new PicoException($"Matrix did not respond within {pico.TimeoutSeconds} s", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new PicoException($"Matrix is unreachable ({ex.Message})", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    logger.LogWarning("Pico returned {StatusCode}: {Error}", (int)response.StatusCode, error);
                    throw new PicoException($"Matrix returned {(int)response.StatusCode}: {error}");
                }
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
