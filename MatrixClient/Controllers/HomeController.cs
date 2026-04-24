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
		var dataRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data");
		var groups = Directory.GetDirectories(dataRoot)
			.OrderBy(d => d)
			.ToDictionary(
				d => Path.GetFileName(d)!,
				d => Directory.GetFiles(d, "*.*")
					.OrderBy(f => f)
					.Select(f => $"{Path.GetFileName(d)}/{Path.GetFileName(f)}")
					.ToArray()
			);
		return View(groups);
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
