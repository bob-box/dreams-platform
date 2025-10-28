using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Controllers;

public class HomeController(ILogger<HomeController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    SignInManager<User> signInManager)
    : BaseController(context, authorizationService, userManager)
{
    private readonly ILogger<HomeController> _logger = logger;

    public IActionResult Index()
    {
        if (TryGetCloudID(out int cloudID))
        {
            // Go to the dreams list of the current cloud
            //return RedirectToAction("Index", "Dream");
        }
        return View();
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
}
