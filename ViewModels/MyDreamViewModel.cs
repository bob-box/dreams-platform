using Borderland.Dreams.Enums;

namespace Borderland.Dreams.ViewModels;

public class MyDreamViewModel
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public DreamStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CloudName { get; set; } = string.Empty;
    public string CloudSlug { get; set; } = string.Empty;
}
