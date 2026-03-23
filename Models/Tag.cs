using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.Models;

public class Tag
{
    public int ID { get; set; }
    [Required]
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;
    public int CloudID { get; set; }
    public Cloud Cloud { get; set; } = null!;
}
