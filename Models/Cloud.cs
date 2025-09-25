using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.Models;

public class Cloud
{
    public int ID { get; set; }

    /// <summary>
    /// Name is a unique index, see ApplicationDbContext.OnModelCreating()
    /// </summary>
    [Required]
    public string Name { get; set; }

    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation property
    public List<Dream> Dreams { get; set; } = new List<Dream>();
}
