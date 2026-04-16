using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Comment : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }    
    
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    public int DreamID { get; set; }
    public string Text { get; set; } = string.Empty;
    public string UserID { get; set; } = null!;


    // Links to the dream and user who made the comment
    public Dream Dream { get; set; } = null!;
    public User User { get; set; } = null!;
}
