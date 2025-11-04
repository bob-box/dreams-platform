namespace Borderland.Dreams.ViewModels;

public class DreamerViewModel : IDreamersForAuthorization
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    public string UserID { get; set; }
    public bool IsMainDreamer { get; set; }
    public string FullName { get; set; }
    public string UserName { get; set; }
}