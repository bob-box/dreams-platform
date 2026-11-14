using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum CommentServiceStatus
{
    Success,
    DreamNotFound,
    NotMember,
    InvalidBody
}

public record CommentServiceResult(
    CommentServiceStatus Status,
    int? CommentId = null,
    string? ErrorMessage = null);

public interface ICommentService
{
    Task<IReadOnlyList<CommentViewModel>> GetForDream(int dreamId);
    Task<CommentServiceResult> Post(int dreamId, string authorId, string? body);
}
