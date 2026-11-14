using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public record EmailManagementResult(
    EmailManagementStatus Status,
    EmailViewModel? ViewModel = null,
    Dictionary<string, List<string>>? Errors = null,
    string? Message = null);

public interface IEmailManagementService
{
    Task<EmailSettingsViewModel> GetSettingsAsync();
    Task SaveSettingsAsync(EmailSettingsViewModel model);
    Task<List<EmailListItemViewModel>> GetEmailsAsync();
    Task<EmailViewModel> BuildCreateViewModelAsync();
    Task<EmailManagementResult> CreateEmailAsync(EmailViewModel model);
    Task<EmailManagementResult> GetForEditAsync(int id);
    Task<EmailManagementResult> UpdateEmailAsync(int id, EmailViewModel model);
    Task<EmailManagementResult> CancelEmailAsync(int id);
    Task PopulateEmailOptionsAsync(EmailViewModel model);
}
