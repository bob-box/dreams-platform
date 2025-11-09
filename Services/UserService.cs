using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class UserService(ApplicationDbContext _context)
{
    public async Task<List<ManageUsersViewModel>> GetUsersInCloud(int cloudId)
    {
        var list = await (from user in _context.Users
                          join userRoles in _context.UserRoles on user.Id equals userRoles.UserId
                          join role in _context.Roles on userRoles.RoleId equals role.Id
                          join wallet in _context.Wallet on user.Id equals wallet.UserID
                          join claim in _context.UserClaims on user.Id equals claim.UserId
                          where claim.ClaimType == Constants.JOINED_CLOUD_CLAIM
                              && claim.ClaimValue == cloudId.ToString()
                              && wallet.CloudID == cloudId
                          select new ManageUsersViewModel
                          {
                              Id = user.Id,
                              UserName = user.UserName,
                              Email = user.Email,
                              FullName = user.FullName,
                              Wallet = wallet,
                              Role = role.Name
                          })
                    .ToListAsync();

        return list;
    }
}
