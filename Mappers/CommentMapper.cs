using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class CommentMapper
{
    public static CommentViewModel ToViewModel(this Comment model)
    {
        if (model == null) return null!;
        return new CommentViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            UserID = model.UserID,
            Text = model.Text,
            CreatedDate = model.CreatedDate
        };
    }

    public static Comment ToModel(this CommentViewModel vm)
    {
        if (vm == null) return null!;
        return new Comment
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            UserID = vm.UserID,
            Text = vm.Text,
            CreatedDate = vm.CreatedDate
        };
    }
}