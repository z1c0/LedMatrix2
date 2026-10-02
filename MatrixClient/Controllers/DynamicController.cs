using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class DynamicController(DynamicImageService dynamicImages) : Controller
{
    // Content changes every minute, so the gallery thumbnails must never come from the browser cache
    [HttpGet]
    [Route("Dynamic/Preview/{mode}")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Preview(string mode)
    {
        if (!DynamicImageService.IsDynamic(mode)) return NotFound();

        using var img = await dynamicImages.RenderAsync(mode);
        using var ms  = new MemoryStream();
        await img.SaveAsPngAsync(ms);
        return File(ms.ToArray(), "image/png");
    }
}
