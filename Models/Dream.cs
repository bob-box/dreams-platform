using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class Dream : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public DreamStatus Status { get; set; } = DreamStatus.Draft;

    [Display(Name = "Budget")]
    public ICollection<BudgetItem> BudgetItems { get; set; } = [];
    public int CloudID { get; set; }

    public Cloud Cloud { get; set; } = null!;
    public ICollection<Fund> Funds { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Dreamer> Dreamers { get; set; } = [];
}
