using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Models;

public class Dream
{
    public int ID { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    [DataType(DataType.DateTime)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DreamStatus Status { get; set; } = DreamStatus.Draft;

    [Display(Name = "Budget")]
    public ICollection<BudgetItem> BudgetItems { get; set; } = [];
    public int CloudID { get; set; }

    public Cloud Cloud { get; set; }
    public ICollection<Fund> Funds { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Dreamer> Dreamers { get; set; } = [];
}
