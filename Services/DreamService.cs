using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class DreamService(ApplicationDbContext _context)
{
    /// <summary>
    /// Includes related entities (so you can run AuthorizationService checks).
    /// </summary>
    /// <param name="cloudId">Cloud ID that the dream belongs to</param>
    /// <returns></returns>
    public IQueryable<Dream> GetDreamsByCloudIdBaseQuery(int cloudId)
    {
        return _context.Dream
            .Include(d => d.BudgetItems)
            .Include(d => d.Dreamers).ThenInclude(d => d.User)
            .Where(d => d.CloudID == cloudId);
    }
}