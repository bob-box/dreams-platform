namespace Borderland.Dreams.Models;

public class CloudInfo
{
    public int ID { get; set; }
    public int CloudID { get; set; }
    public required string Content { get; set; }
    public Cloud Cloud { get; set; } = null!;
}