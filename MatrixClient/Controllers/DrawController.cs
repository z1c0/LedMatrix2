using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public partial class DrawController(PixelSenderService sender, ILogger<DrawController> logger) : SendControllerBase(logger)
{
    public IActionResult Index() => View();

    [HttpPost]
    [Route("Draw/Send")]
    public Task<IActionResult> Send(string? pixels)
    {
        if (pixels is null || !PixelPayload().IsMatch(pixels))
        {
            return Task.FromResult<IActionResult>(BadRequest(new
            {
                success = false,
                error = $"Expected 6144 hex chars, got {pixels?.Length ?? 0} chars"
                        + (pixels?.Length == 6144 ? " with non-hex characters" : "")
            }));
        }

        return SendAndReport(() => sender.SendRawAsync(pixels), "Draw/Send");
    }

    // 32×32 pixels × RRGGBB
    [GeneratedRegex("^[0-9A-Fa-f]{6144}$")]
    private static partial Regex PixelPayload();
}
