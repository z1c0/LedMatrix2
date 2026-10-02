using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class PixelController(PixelSenderService sender, ILogger<PixelController> logger) : SendControllerBase(logger)
{
    [HttpPost]
    [Route("Pixels/SendToMatrix")]
    public Task<IActionResult> SendToMatrix(string fileName) =>
        SendAndReport(() => sender.SendAsync(fileName), "SendToMatrix");
}
