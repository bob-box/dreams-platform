namespace Borderland.Dreams.Controllers;

public class FindDreamerRequest
{
    public int DreamID { get; set; }
    public string Username { get; set; } = string.Empty;
}