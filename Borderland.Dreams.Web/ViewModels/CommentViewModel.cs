namespace Borderland.Dreams.ViewModels;

public class CommentViewModel
{
    public int Id { get; set; }
    public int DreamId { get; set; }
    public int? ParentCommentId { get; set; }
    public string? AuthorId { get; set; }
    public string AuthorName { get; set; } = "[deleted user]";
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public List<CommentViewModel> Replies { get; set; } = [];
    public CommentPostViewModel? ReplyForm { get; set; }
    public CommentPostViewModel? EditForm { get; set; }
}