using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class DrawController(PixelSenderService sender, ILogger<DrawController> logger) : Controller
{
    public IActionResult Index() => View();

    [HttpPost]
    [Route("Draw/Send")]
    public async Task<IActionResult> Send(string pixels)
    {
        try
        {
            const int expected = 32 * 32 * 6;
            if (string.IsNullOrEmpty(pixels) || pixels.Length != expected)
                throw new Exception($"Expected {expected} hex chars, got {pixels?.Length ?? 0}");

            await sender.SendRawAsync(pixels);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Draw/Send");
            return Json(new { success = false, error = e.Message });
        }
        return Json(new { success = true });
    }
}
