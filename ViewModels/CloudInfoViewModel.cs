using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class CloudInfoViewModel
{
    [StringLength(50000)]
    public string Content { get; set; } = "";
}