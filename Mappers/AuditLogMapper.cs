using System.Text.Json;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class AuditLogMapper(IAppTime appTime)
{
    public AuditLogItemViewModel ToViewModel(AuditLogEntry model, string cloudName)
    {
        return new AuditLogItemViewModel
        {
            CreatedAt = appTime.ToLocal(model.CreatedAtUtc),
            CloudID = model.CloudID,
            CloudName = cloudName,
            EntityType = model.EntityType,
            EntityID = model.EntityID,
            EntityName = model.EntityName,
            Action = model.Action,
            UserName = model.UserName,
            Changes = ReadChanges(model.Changes)
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
