using Borderland.Dreams.ViewModels;

public interface IDreamersForAuthorization
{
    public string UserID { get; set; }
    public bool IsMainDreamer { get; set; }
}