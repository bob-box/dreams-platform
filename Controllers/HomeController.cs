using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Utils;

namespace Borderland.Dreams.Controllers;

public class HomeController(
    // ILogger<HomeController> logger,
    ICloudService cloudService,
    InMemoryLogStore logStore
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var viewModels = await cloudService.GetAll();

        return View(viewModels);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize(Roles = Constants.AdminRole)]
    public IActionResult Logs() => View(logStore.Entries.Reverse().ToList());

}
