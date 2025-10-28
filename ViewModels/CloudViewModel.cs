using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class CloudViewModel
{
    public int ID { get; set; }
    public string Name { get; set; }
    [DataType(DataType.Date)]
    public DateTime CreatedDate { get; set; }
    public List<DreamViewModel>? Dreams { get; set; }
}