using Microsoft.AspNetCore.Mvc;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class AutoSendController(AutoSendService autoSend) : Controller
{
    [HttpPost]
    [Route("AutoSend/Enable")]
    public IActionResult Enable()
    {
        autoSend.Enable();
        return Json(new { enabled = true });
    }

    [HttpPost]
    [Route("AutoSend/Disable")]
    public IActionResult Disable()
    {
        autoSend.Disable();
        return Json(new { enabled = false });
    }

    [HttpGet]
    [Route("AutoSend/Status")]
    public IActionResult Status()
    {
        var rule = autoSend.GetActiveRule();
        return Json(new
        {
            enabled = autoSend.IsEnabled,
            activeRule = rule?.Name,
            intervalMinutes = autoSend.Config.IntervalMinutes
        });
    }
}
