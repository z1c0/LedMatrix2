using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;
using SixLabors.ImageSharp;

namespace MatrixClient.Controllers;

public abstract class SendControllerBase(ILogger logger) : Controller
{
    protected async Task<IActionResult> SendAndReport(Func<Task> send, string operation)
    {
        try
        {
            await send();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            // Our own exceptions carry messages written for the UI; anything else stays in the log
            var (status, message) = ex switch
            {
                ArgumentException     => (StatusCodes.Status400BadRequest, ex.Message),
                FileNotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                InvalidDataException  => (StatusCodes.Status422UnprocessableEntity, ex.Message),
                ImageFormatException  => (StatusCodes.Status422UnprocessableEntity, "Not a supported image file"),
                PicoException         => (StatusCodes.Status502BadGateway, ex.Message),
                _                     => (StatusCodes.Status500InternalServerError, "Unexpected error, see server log"),
            };

            if (status >= 500)
                logger.LogError(ex, "{Operation} failed", operation);
            else
                logger.LogWarning("{Operation} rejected: {Message}", operation, ex.Message);

            return StatusCode(status, new { success = false, error = message });
        }
    }
}
