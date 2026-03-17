using Borderland.Dreams.Data;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class DreamService(ApplicationDbContext _context, ICloudNameResolver cloudNameService)
{
    /// <summary>
    /// Includes related entities (so you can run AuthorizationService checks).
    /// </summary>
    /// <param name="cloudId">Cloud ID that the dream belongs to</param>
    /// <returns></returns>
    public IQueryable<Dream> GetDreamsByCloudIdBaseQuery()
    {
        int cloudId = cloudNameService.CloudID;

        return _context.Dream
            .Include(d => d.BudgetItems)
            .Include(d => d.Dreamers).ThenInclude(d => d.User)
            .Include(d => d.Funds).ThenInclude(d => d.User)
            .Where(d => d.CloudID == cloudId);
    }
}