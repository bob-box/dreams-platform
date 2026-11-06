using Borderland.Dreams.Models;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Borderland.Dreams.Areas.Identity.Pages.Account.Manage;

public class MyDreamsModel(IDreamService dreamService, UserManager<User> userManager) : PageModel
{
    public IReadOnlyList<MyDreamViewModel> Dreams { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");

        Dreams = await dreamService.GetDreamsByUserId(user.Id);
        return Page();
    }
}
