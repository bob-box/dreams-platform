using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum AdminCloudServiceStatus
{
    Success,
    NotFound
}

public interface IAdminCloudService
{
    Task Create(CloudViewModel viewModel);
    Task<AdminCloudServiceStatus> Update(CloudViewModel viewModel);
    Task<AdminCloudServiceStatus> Delete(int id);
}