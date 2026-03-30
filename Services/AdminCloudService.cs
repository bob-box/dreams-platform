using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class AdminCloudService(
    ApplicationDbContext context,
    ICloudCache cloudCache) : IAdminCloudService
{
    public async Task Create(CloudViewModel viewModel)
    {
        var cloud = viewModel.ToModel();
        context.Add(cloud);
        await context.SaveChangesAsync();
    }

    public async Task<AdminCloudServiceStatus> Update(CloudViewModel viewModel)
    {
        var cloud = await context.Cloud.FindAsync(viewModel.ID);
        if (cloud == null)
            return AdminCloudServiceStatus.NotFound;

        var oldName = cloud.Name;
        cloud.Name = viewModel.Name;
        cloud.TotalFundsAvailable = viewModel.TotalFundsAvailable;

        try
        {
            await context.SaveChangesAsync();

            if (!oldName.Equals(viewModel.Name, StringComparison.OrdinalIgnoreCase))
                cloudCache.Remove(oldName);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Cloud.AnyAsync(e => e.ID == viewModel.ID))
                return AdminCloudServiceStatus.NotFound;
            throw;
        }

        return AdminCloudServiceStatus.Success;
    }

    public async Task<AdminCloudServiceStatus> Delete(int id)
    {
        var cloud = await context.Cloud.FindAsync(id);
        if (cloud == null)
            return AdminCloudServiceStatus.NotFound;

        context.Cloud.Remove(cloud);
        cloudCache.Remove(cloud.Name);
        await context.SaveChangesAsync();

        return AdminCloudServiceStatus.Success;
    }
}