using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class CommentPostViewModel
{
    public int DreamId { get; set; }

    [Required(ErrorMessage = "No comment written.")]
    [StringLength(1000, ErrorMessage = "Comments must be 1000 characters or fewer.")]
    public string? Body { get; set; }
}
