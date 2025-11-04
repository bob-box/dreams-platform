namespace Borderland.Dreams.ViewModels;

public class DreamerViewModel
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    public string UserID { get; set; }
    public bool IsMainDreamer { get; set; }
    public UserViewModel? User { get; set; }
}