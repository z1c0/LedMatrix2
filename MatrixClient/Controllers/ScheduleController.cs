using Microsoft.AspNetCore.Mvc;
using MatrixClient.Models;

namespace MatrixClient.Controllers;

public class ScheduleController(AutoSendConfig config) : Controller
{
    public IActionResult Index() => View(config);
}
