using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class DynamicController : Controller
{
    private static readonly HashSet<string> KnownModes = ["clock", "calendar"];

    [HttpGet]
    [Route("Dynamic/Preview/{mode}")]
    public IActionResult Preview(string mode)
    {
        Image<Rgba32> img = mode switch
        {
            "clock"    => ClockRenderer.Render(),
            "calendar" => CalendarRenderer.Render(),
            _          => null!
        };
        if (img is null) return NotFound();

        using (img)
        {
            var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return File(ms.ToArray(), "image/png");
        }
    }

    public static bool IsDynamic(string path) => KnownModes.Contains(path);
}
