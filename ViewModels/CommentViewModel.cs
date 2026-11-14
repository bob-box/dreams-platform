namespace Borderland.Dreams.ViewModels;

public class CommentViewModel
{
    public int Id { get; set; }
    public int DreamId { get; set; }
    public string? AuthorId { get; set; }
    public string AuthorName { get; set; } = "[deleted user]";
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}