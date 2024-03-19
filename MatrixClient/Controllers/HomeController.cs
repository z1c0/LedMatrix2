using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MatrixClient.Models;

namespace MatrixClient.Controllers;

public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index()
	{
		var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
		var imagePaths = Directory.GetFiles(folderPath, "*.*")
			.Select(p => Url.Content("~/data/" + Path.GetFileName(p)))
			.ToArray();
		return View(imagePaths);
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
