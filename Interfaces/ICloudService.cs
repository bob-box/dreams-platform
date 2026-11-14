using System.Security.Claims;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum JoinCloudStatus
{
    Success,
    CloudNotFound,
    UserNotFound,
    AlreadyJoined
}
public enum CloudServiceStatus
{
    Success,
    NotFound,
    SlugAlreadyInUse,
}

public record JoinCloudResult(JoinCloudStatus Status);

public interface ICloudService
{
    Task<CloudServiceStatus> Create(CloudViewModel viewModel);
    Task<CloudServiceStatus> Update(CloudViewModel viewModel);
    Task<CloudServiceStatus> Delete(int id);
    Task<CloudViewModel> GetById(int id);
    Task<List<CloudViewModel>> GetAll();
    Task<CloudInfoViewModel> GetInfo(int cloudId);
    Task UpdateInfo(int cloudId, CloudInfoViewModel viewModel);
    Task<JoinCloudResult> Join(int cloudId, string userId);
    Task<bool> UserHasJoinedCloud(string userId);
    Task<bool> UserHasJoinedCloud(ClaimsPrincipal user);
    Task<bool> UserHasJoinedCloud(User user);
    Task<bool> IsDreamingPeriodActive(int cloudID);
}