using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum JoinCloudStatus
{
    Success,
    CloudNotFound,
    UserNotFound,
    AlreadyJoined
}

public record JoinCloudResult(JoinCloudStatus Status);

public interface ICloudService
{
    Task<CloudViewModel?> GetById(int id);
    Task<List<CloudViewModel>> GetAll();
    Task<JoinCloudResult> Join(int cloudId, string userId);
    Task<bool> UserHasJoinedCloud(string userId);

}