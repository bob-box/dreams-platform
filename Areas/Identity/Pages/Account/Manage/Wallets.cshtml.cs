using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Areas.Identity.Pages.Account.Manage;

public class WalletsModel(ApplicationDbContext context, UserManager<User> userManager) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    public List<WalletEntry> Wallets { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        Wallets = await _context.Wallet
            .Where(w => w.UserID == user.Id)
            .GroupBy(w => new { w.CloudID, w.Cloud.Name, w.Cloud.Slug })
            .Select(g => new WalletEntry
            {
                CloudId = g.Key.CloudID,
                CloudName = g.Key.Name,
                CloudSlug = g.Key.Slug,
                Balance = g.Sum(w => w.Balance)
            })
            .OrderBy(w => w.CloudName)
            .ToListAsync();

        return Page();
    }

    public class WalletEntry
    {
        public int CloudId { get; set; }
        public string CloudName { get; set; } = string.Empty;
        public string CloudSlug { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}