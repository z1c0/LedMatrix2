using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class DisplayController(PicoStatus status) : Controller
{
    // "online", "offline" or "unknown" (nothing sent or probed since startup)
    [HttpGet]
    [Route("Display/Status")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status() => Json(new { display = StateName(status.Current) });

    public static string StateName(PicoStatus.State state) => state.ToString().ToLowerInvariant();
}
