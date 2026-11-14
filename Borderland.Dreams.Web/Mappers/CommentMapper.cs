using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class CommentMapper(IAppTime appTime)
{
    public CommentViewModel ToViewModel(Comment model)
    {
        return new CommentViewModel
        {
            Id = model.ID,
            DreamId = model.DreamID,
            AuthorId = model.AuthorID,
            AuthorName = model.Author?.Name ?? "[deleted user]",
            Body = model.Body,
            CreatedAt = appTime.ToLocal(model.CreatedAtUtc)
        };
    }

    public Comment ToModel(CommentViewModel vm)
    {
        return new Comment
        {
            ID = vm.Id,
            DreamID = vm.DreamId,
            AuthorID = vm.AuthorId,
            Body = vm.Body,
        };
    }
}