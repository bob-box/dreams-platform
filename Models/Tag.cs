using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Tag : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
        
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    [Required]
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;
    public int CloudID { get; set; }
    public Cloud Cloud { get; set; } = null!;
}
