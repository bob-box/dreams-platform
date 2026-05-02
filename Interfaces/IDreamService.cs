using System.Security.Claims;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum DreamServiceStatus
{
    Success,
    NotFound,
    NotAllowed
}

public record DreamServiceResult(DreamServiceStatus Status, DreamViewModel? Dream = null);

public interface IDreamService
{
    Task<DreamFilterViewModel> BuildDreamFilterViewModel(
        string? searchText,
        DreamStatus? statusFilter,
        int? tagFilter,
        DreamSortOrder sortOrder,
        string? currentUserId,
        bool isAdmin);

    Task<int> Create(DreamViewModel viewModel);
    Task<DreamServiceResult> Update(DreamViewModel viewModel);
    Task<DreamServiceResult> GetForDelete(int id);
    Task<DreamServiceResult> Delete(int id);
    Task<DreamServiceResult> GetQuick(int id);
    Task<DreamServiceResult> GetFull(int id);
    bool DreamExists(int iD, int cloudID);
    Task<bool> CanUserEditDreamers(DreamViewModel dream, ClaimsPrincipal user);
}