using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class TagViewModel
{
    private string _title = string.Empty;
    public int ID { get; set; }
    [Required]
    [MaxLength(50)]
    public string Title
    {
        get => _title;
        set => _title = value.Trim();
    }
    public int CloudID { get; set; }
}