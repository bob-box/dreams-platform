namespace Borderland.Dreams.Models;

public class DreamTag
{
    public int DreamID { get; set; }
    public int TagID { get; set; }

    public required Dream Dream { get; set; }
    public required Tag Tag { get; set; }
}
