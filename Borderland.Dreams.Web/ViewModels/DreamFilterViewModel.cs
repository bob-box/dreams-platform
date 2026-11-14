using Borderland.Dreams.Enums;

namespace Borderland.Dreams.ViewModels;

public class DreamFilterViewModel
{
    public string? SearchText { get; set; }
    public DreamStatus? StatusFilter { get; set; }
    public int? TagFilter { get; set; }
    public DreamSortOrder SortOrder { get; set; } = DreamSortOrder.NameAsc;

    public List<DreamViewModel> Dreams { get; set; } = [];
    public List<TagViewModel> AvailableTags { get; set; } = [];
}
