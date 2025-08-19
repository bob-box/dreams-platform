using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Models;

public class Dream
{
    public int ID { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; }
    public DreamStatus Status { get; set; }

    public int? BudgetID { get; set; }
    public DreamBudget? Budget { get; set; }

    public List<Fund> Funds { get; set; } = new List<Fund>();
    public List<Comment> Comments { get; set; } = new List<Comment>();
    public List<Expense> Expenses { get; set; } = new List<Expense>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public List<Dreamer> Dreamers { get; set; } = new List<Dreamer>();
}
