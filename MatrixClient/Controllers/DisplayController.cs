using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;
using SixLabors.ImageSharp;

namespace MatrixClient.Controllers;

public class DisplayController(PicoStatus status) : Controller
{
    // "online", "offline" or "unknown" (nothing sent or probed since startup), plus the last send attempt
    [HttpGet]
    [Route("Display/Status")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status()
    {
        var attempt = status.LastAttempt;
        return Json(new
        {
            display  = StateName(status.Current),
            lastSend = attempt is null ? null : new
            {
                at         = attempt.At.ToString("yyyy-MM-dd HH:mm:ss"),
                content    = attempt.Content.Path ?? "drawing",
                success    = attempt.Success,
                result     = attempt.Result,
                durationMs = attempt.DurationMs,
                // The timestamp only busts the browser cache when a new attempt replaces the image
                image      = Url.Action(nameof(LastSent), new { t = attempt.At.ToUnixTimeMilliseconds() }),
            },
        });
    }

    // The payload of the last send attempt as a 32x32 PNG, i.e. exactly what the matrix was asked to show
    [HttpGet]
    [Route("Display/LastSent")]
    public IActionResult LastSent()
    {
        var hex = status.LastAttempt?.Content.Hex;
        using var image = hex is null ? null : PixelSenderService.FromHex(hex);
        if (image is null) return NotFound();

        using var png = new MemoryStream();
        image.SaveAsPng(png);
        return File(png.ToArray(), "image/png");
    }

    public static string StateName(PicoStatus.State state) => state.ToString().ToLowerInvariant();
}
