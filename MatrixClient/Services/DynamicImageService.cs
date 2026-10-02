using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

// Images rendered on demand instead of read from the gallery; usable anywhere a gallery path is
public class DynamicImageService(WeatherService weatherService, TimeProvider time)
{
    private static readonly Dictionary<string, Func<DynamicImageService, Task<Image<Rgba32>>>> Renderers = new()
    {
        ["clock"]    = s => Task.FromResult(ClockRenderer.Render(s.LocalNow)),
        ["calendar"] = s => Task.FromResult(CalendarRenderer.Render(s.LocalNow)),
        ["weather"]  = s => WeatherRenderer.RenderAsync(s.Weather),
    };

    public static IReadOnlyCollection<string> Modes => Renderers.Keys;

    public static bool IsDynamic(string mode) => Renderers.ContainsKey(mode);

    private DateTime LocalNow => time.GetLocalNow().DateTime;
    private WeatherService Weather => weatherService;

    public Task<Image<Rgba32>> RenderAsync(string mode) =>
        Renderers.TryGetValue(mode, out var render)
            ? render(this)
            : throw new ArgumentException($"Unknown dynamic mode: {mode}");
}
