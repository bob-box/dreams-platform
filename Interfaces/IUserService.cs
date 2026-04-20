using Borderland.Dreams.Enums;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public interface IUserService
{
    /// <summary> Gets a list of users in the current cloud, optionally filtered by a search term.
    /// </summary>
    /// <param name="filter">An optional search term to filter users by name or email.</param>
    /// <returns>A list of users in the current cloud that match the filter criteria.</returns>
    Task<IEnumerable<UserViewModel>> GetAll(string filter = "");

    /// <summary> Gets the count of users in the current cloud.
    /// </summary> 
    /// <returns>The count of users in the current cloud.</returns>
    Task<int> Count();

    /// <summary>
    /// Updates the wallet balance for a user or all users in the current cloud.
    /// </summary>
    /// <param name="userId">The ID of the user to update, or null/empty to update all users in the cloud.</param>
    /// <param name="action">The action to perform on the wallet balance (set or add).</param>
    /// <param name="amount">The amount to set or add to the wallet balance.</param>
    Task UpdateWallet(string userId, WalletAction action, int amount);

    /// <summary>
    /// Updates the role of a user in the current cloud.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="currentUserId">The ID of the current user performing the update.</param>
    /// <param name="role">The new role to assign to the user.</param>
    Task UpdateRole(string userId, string currentUserId, string role);
}