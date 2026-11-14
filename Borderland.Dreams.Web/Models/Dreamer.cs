namespace Borderland.Dreams.Models;

public class Dreamer
{
    public int ID { get; set; }
    public int DreamID { get; set; }
    public bool IsMainDreamer { get; set; }
    public string UserID { get; set; } = null!;

    public User User { get; set; } = null!;
    // public Dream Dream { get; set; } = null!;
}
