using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public class PixelSenderService(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, WeatherService weatherService, ILogger<PixelSenderService> logger)
{
    private const string PicoUrl = "http://192.168.1.225/";

    public async Task SendAsync(string relativePath)
    {
        using var image = await ResolveImageAsync(relativePath);
        if (image.Width != 32 || image.Height != 32)
            throw new Exception($"Unexpected image dimensions: {image.Width}x{image.Height}");

        var sb = new StringBuilder(32 * 32 * 6);
        for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
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

        var content  = new StringContent(sb.ToString(), Encoding.UTF8, "application/text");
        var response = await httpClientFactory.CreateClient().PostAsync(PicoUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Pico returned {StatusCode}: {Error}", (int)response.StatusCode, error);
            throw new Exception($"Pico returned {(int)response.StatusCode}: {error}");
        }
    }

    public async Task SendRawAsync(string hexPayload)
    {
        var content  = new StringContent(hexPayload, Encoding.UTF8, "application/text");
        var response = await httpClientFactory.CreateClient().PostAsync(PicoUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Pico returned {StatusCode}: {Error}", (int)response.StatusCode, error);
            throw new Exception($"Pico returned {(int)response.StatusCode}: {error}");
        }
    }

    private Task<Image<Rgba32>> ResolveImageAsync(string path) => path switch
    {
        "clock"    => Task.FromResult(ClockRenderer.Render()),
        "calendar" => Task.FromResult(CalendarRenderer.Render()),
        "weather"  => WeatherRenderer.RenderAsync(weatherService),
        _          => Task.FromResult(Image.Load<Rgba32>(Path.Combine(env.WebRootPath, "data", path)))
    };
}
