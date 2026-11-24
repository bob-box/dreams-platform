using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class CommentPostViewModel
{
    public const int MaxBodyLength = 1000;

    public int DreamId { get; set; }
    public int? ParentCommentId { get; set; }
    public int? CommentId { get; set; }
    public string? ErrorMessage { get; set; }

    [Required(ErrorMessage = "No comment written.")]
    [StringLength(MaxBodyLength, ErrorMessage = "Comments must be 1000 characters or fewer.")]
    public string? Body { get; set; }
}
