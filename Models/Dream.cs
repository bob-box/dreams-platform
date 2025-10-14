using System.ComponentModel.DataAnnotations;
using Borderland.Dreams.Enums;

namespace Borderland.Dreams.Models;

public class Dream : IDreamWithDreamersForAuthorization
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DreamStatus Status { get; set; } = DreamStatus.Draft;

    [Display(Name = "Budget")]
    public List<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
    public int CloudID { get; set; }

    public List<Fund> Funds { get; set; } = new List<Fund>();
    public List<Comment> Comments { get; set; } = new List<Comment>();
    public List<Expense> Expenses { get; set; } = new List<Expense>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public List<Dreamer> Dreamers { get; set; } = new List<Dreamer>();
}
