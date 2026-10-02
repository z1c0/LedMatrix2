using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MatrixClient.Models;
using MatrixClient.Services;

namespace MatrixClient.Controllers;

public class HomeController(GalleryService gallery) : Controller
{
    public IActionResult Index() => View(gallery.GetCategories());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
