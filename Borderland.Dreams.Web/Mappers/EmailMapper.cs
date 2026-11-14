using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class EmailMapper(IAppTime appTime)
{
    public EmailListItemViewModel ToViewModel(Email model)
    {
        return new EmailListItemViewModel
        {
            ID = model.ID,
            Subject = model.Subject,
            Audience = model.Audience,
            DreamStatus = model.DreamStatus,
            Status = model.Status,
            ScheduledAt = appTime.ToLocal(model.ScheduledAtUtc),
            SentAt = appTime.ToLocal(model.SentAtUtc),
            RecipientCount = model.RecipientCount,
            Error = model.Error
        };
    }
}
