using Microsoft.AspNetCore.Mvc;
using MatrixClient.Imaging;
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

        var img = await dynamicImages.RenderAsync(mode);
        return File(img.ToPng(), "image/png");
    }
}
