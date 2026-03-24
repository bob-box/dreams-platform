using Borderland.Dreams.Authorization;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Borderland.Dreams.Extensions;

public static class SelectListHelper
{
    public static IEnumerable<SelectListItem> GetUserRoleOptions(string? selectedRole = null)
    {
        var roles = new[]
        {
            Constants.MemberRole,
            Constants.AdminRole
        };

        return roles.Select(role => new SelectListItem
        {
            Value = role,
            Text = role,
            Selected = string.Equals(selectedRole, role, StringComparison.Ordinal)
        });
    }

    public static IEnumerable<SelectListItem> GetTagOptions(IEnumerable<Tag>? availableTags, IEnumerable<int>? selectedTagIds = null)
    {
        if (availableTags == null)
            return [];

        var selectedIds = selectedTagIds?.ToHashSet() ?? [];

        return availableTags
            .OrderBy(tag => tag.Title)
            .Select(tag => new SelectListItem
            {
                Value = tag.ID.ToString(),
                Text = tag.Title,
                Selected = selectedIds.Contains(tag.ID)
            });
    }
}