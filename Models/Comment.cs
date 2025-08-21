namespace Borderland.Dreams.Models;

public class Comment
{
    public int ID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public int DreamID { get; set; }
    public string Text { get; set; } = string.Empty;
    public string UserID { get; set; } = null!;


    // Links to the dream and user who made the comment
    public Dream Dream { get; set; } = null!;
    public User User { get; set; } = null!;
}
