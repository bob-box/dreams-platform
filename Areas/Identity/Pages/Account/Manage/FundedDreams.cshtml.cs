using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Areas.Identity.Pages.Account.Manage;

public class FundedDreamsModel(ApplicationDbContext context, UserManager<User> userManager) : PageModel
{

    public List<CloudFundingGroup> Clouds { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        var rawGroups = await context.Fund
            .Where(f => f.UserID == user.Id)
            .GroupBy(f => new { f.Dream.CloudID, f.Dream.Cloud.Name })
            .Select(g => new
            {
                CloudId = g.Key.CloudID,
                CloudName = g.Key.Name,
                TotalAmountFunded = g.Sum(f => f.Amount),
                Dreams = g.Select(f => new
                {
                    ID = f.Dream.ID,
                    Name = f.Dream.Name,
                    AmountFundedByMe = f.Amount
                }).ToList()
            })
            .ToListAsync(); // <-- EF Core executes SQL here, everything above is translated to SQL

        // Map to model classes in memory
        Clouds = rawGroups.Select(g => new CloudFundingGroup
        {
            CloudId = g.CloudId,
            CloudName = g.CloudName,
            TotalAmountFunded = g.TotalAmountFunded,
            Dreams = g.Dreams
                .DistinctBy(d => d.ID)
                .Select(d => new FundedDreamEntry
                {
                    ID = d.ID,
                    Name = d.Name,
                    AmountFundedByMe = d.AmountFundedByMe
                }).ToList()
        }).ToList();

        return Page();
    }

    public class CloudFundingGroup
    {
        public int CloudId { get; set; }
        public string CloudName { get; set; } = string.Empty;
        public decimal TotalAmountFunded { get; set; }
        public List<FundedDreamEntry> Dreams { get; set; } = [];
    }

    public class FundedDreamEntry
    {
        public int ID { get; set; }
        public required string Name { get; set; }
        public decimal AmountFundedByMe { get; set; }
    }
}