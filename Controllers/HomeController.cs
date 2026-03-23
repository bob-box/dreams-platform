using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Borderland.Dreams.Models;
using Borderland.Dreams.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Mappers;

namespace Borderland.Dreams.Controllers;

public class HomeController(ILogger<HomeController> logger,
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<User> userManager,
    SignInManager<User> signInManager)
    : BaseController(context, authorizationService, userManager)
{
    private readonly SignInManager<User> _signInManager = signInManager;
    
    private readonly ILogger<HomeController> _logger = logger;

    public async Task<IActionResult> Index()
    {
        var clouds = await _context.Cloud
            .Include(c => c.Dreams)
            .ToListAsync();

        var viewModels = clouds.Select(c => c.ToViewModel()).ToList();

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
}
