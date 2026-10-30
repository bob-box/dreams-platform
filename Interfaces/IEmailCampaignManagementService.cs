using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public record EmailCampaignManagementResult(
    EmailCampaignManagementStatus Status,
    EmailCampaignViewModel? ViewModel = null,
    Dictionary<string, List<string>>? Errors = null,
    string? Message = null);

public interface IEmailCampaignManagementService
{
    Task<EmailSettingsViewModel> GetSettingsAsync();
    Task SaveSettingsAsync(EmailSettingsViewModel model);
    Task<List<EmailCampaign>> GetCampaignsAsync();
    Task<EmailCampaignViewModel> BuildCreateViewModelAsync();
    Task<EmailCampaignManagementResult> CreateCampaignAsync(EmailCampaignViewModel model);
    Task<EmailCampaignManagementResult> GetForEditAsync(int id);
    Task<EmailCampaignManagementResult> UpdateCampaignAsync(int id, EmailCampaignViewModel model);
    Task<EmailCampaignManagementResult> CancelCampaignAsync(int id);
    Task PopulateCampaignOptionsAsync(EmailCampaignViewModel model);
}
