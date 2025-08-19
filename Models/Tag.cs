namespace Borderland.Dreams.Models;

public class Tag
{
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;

    public List<Dream> Dreams { get; set; } = new List<Dream>();
}
