namespace Borderland.Dreams.ViewModels;

public class EmailIndexViewModel
{
    public List<EmailListItemViewModel> Emails { get; set; } = [];
    public DreamWelcomeEmailViewModel DreamWelcomeEmail { get; set; } = new();
}
