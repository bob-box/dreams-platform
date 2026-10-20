using System.Text.Json;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class AuditLogService(ApplicationDbContext context) : IAuditLogService
{
    private const int MaxResults = 500;

    public async Task<AuditLogViewModel> GetAuditLog(
        string? searchText,
        string? entityType,
        string? action,
        int? cloudId,
        DateTime? from,
        DateTime? to)
    {
        // Ignore soft-delete filters so administrators can inspect records belonging to deleted clouds too.
        var clouds = await context.Cloud
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(cloud => cloud.Name)
            .Select(cloud => new AuditLogCloudOption(cloud.ID, cloud.Name))
            .ToListAsync();

        var query = context.AuditLog.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(entry => entry.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(entry => entry.Action == action);
        if (cloudId.HasValue)
            query = query.Where(entry => entry.CloudID == cloudId.Value);
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(entry => entry.CreatedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            var toUtcExclusive = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(entry => entry.CreatedAt < toUtcExclusive);
        }
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            // Search the cloud label as well as the denormalized fields stored on each audit entry.
            var matchingCloudIds = await context.Cloud
                .IgnoreQueryFilters()
                .Where(cloud => cloud.Name.Contains(searchText))
                .Select(cloud => cloud.ID)
                .ToListAsync();
            query = query.Where(entry =>
                entry.EntityName.Contains(searchText) ||
                entry.UserName.Contains(searchText) ||
                entry.EntityType.Contains(searchText) ||
                entry.Action.Contains(searchText) ||
                entry.Changes.Contains(searchText) ||
                matchingCloudIds.Contains(entry.CloudID));
        }

        // Bound the initial results so the admin page stays responsive for long-lived clouds.
        var entries = await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(MaxResults)
            .Join(
                context.Cloud.IgnoreQueryFilters().AsNoTracking(),
                entry => entry.CloudID,
                cloud => cloud.ID,
                (entry, cloud) => new { Entry = entry, CloudName = cloud.Name })
            .ToListAsync();

        return new AuditLogViewModel
        {
            SearchText = searchText,
            EntityType = entityType,
            Action = action,
            CloudID = cloudId,
            From = from,
            To = to,
            Clouds = clouds,
            Entries = entries.Select(row => new AuditLogItemViewModel
            {
                CreatedAt = row.Entry.CreatedAt,
                CloudID = row.Entry.CloudID,
                CloudName = row.CloudName,
                EntityType = row.Entry.EntityType,
                EntityID = row.Entry.EntityID,
                EntityName = row.Entry.EntityName,
                Action = row.Entry.Action,
                UserName = row.Entry.UserName,
                Changes = ReadChanges(row.Entry.Changes)
            }).ToList()
        };
    }

    private static IReadOnlyList<AuditLogChangeViewModel> ReadChanges(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject()
            .Select(property => new AuditLogChangeViewModel(
                property.Name,
                GetString(property.Value, "OldValue"),
                GetString(property.Value, "NewValue")))
            .ToList();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }
}
