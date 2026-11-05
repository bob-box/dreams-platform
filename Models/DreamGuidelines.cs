using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Models;

public class DreamGuidelines : IAutoPopulateCreatedAtOnAdd
{
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}