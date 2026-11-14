using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Comment : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    public string? AuthorID { get; set; }

    public string Body { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTime CreatedAtUtc { get; set; }

    public Dream Dream { get; set; } = null!;
    public User? Author { get; set; }
}
