namespace Borderland.Dreams.ViewModels;

public class CommentViewModel
{
    public int ID { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DreamID { get; set; }
    public string Text { get; set; }
    public string UserID { get; set; }
}