using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public enum CommentServiceStatus
{
    Success,
    DreamNotFound,
    NotMember,
    InvalidBody,
    InvalidParent
}

public record CommentServiceResult(
    CommentServiceStatus Status,
    int? CommentId = null,
    string? ErrorMessage = null);

public interface ICommentService
{
    Task<IReadOnlyList<CommentViewModel>> GetForDream(int dreamId);
    Task<CommentServiceResult> Post(int dreamId, string authorId, string? body);
    Task<CommentServiceResult> Reply(int dreamId, int parentCommentId, string authorId, string? body);
}
