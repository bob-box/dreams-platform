using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Comment : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    public string? AuthorID { get; set; }
    public int? ParentCommentID { get; set; }

    public string Body { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTime CreatedAtUtc { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? EditedUtc { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? DeletedUtc { get; set; }
    public string? DeletedByUserId { get; set; }

    public Dream Dream { get; set; } = null!;
    public User? Author { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
}
