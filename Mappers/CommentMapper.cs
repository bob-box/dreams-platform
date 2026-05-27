using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class CommentMapper(IMapperFacade mapper, IAppTime appTime)
{
    public CommentViewModel ToViewModel(Comment model)
    {
        return new CommentViewModel
        {
            ID = model.ID,
            DreamID = model.DreamID,
            UserID = model.UserID,
            Text = model.Text,
            CreatedAt = appTime.ToLocal(model.CreatedAt)
        };
    }

    public Comment ToModel(CommentViewModel vm)
    {
        return new Comment
        {
            ID = vm.ID,
            DreamID = vm.DreamID,
            UserID = vm.UserID,
            Text = vm.Text,
        };
    }
}