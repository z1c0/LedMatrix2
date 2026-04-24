using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class PixelController(PixelSenderService sender, ILogger<PixelController> logger) : Controller
{
    [HttpPost]
    [Route("Pixels/SendToMatrix")]
    public async Task<IActionResult> SendToMatrix(string fileName)
    {
        try
        {
            fileName = System.Net.WebUtility.UrlDecode(fileName);
            await sender.SendAsync(fileName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "SendToMatrix");
            return Json(new { success = false, error = e.Message });
        }
        return Json(new { success = true });
    }
}
