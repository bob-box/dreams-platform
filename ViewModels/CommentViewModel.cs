using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public class CommentViewModel
{
    public int ID { get; set; }
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [DataType(DataType.DateTime)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int DreamID { get; set; }
    public string Text { get; set; }
    public string UserID { get; set; }
}