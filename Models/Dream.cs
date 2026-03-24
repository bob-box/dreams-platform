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
    public List<BudgetItem> BudgetItems { get; set; } = [];
    public int CloudID { get; set; }

    public Cloud Cloud { get; set; }
    public List<Fund> Funds { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<Dreamer> Dreamers { get; set; } = [];
}
