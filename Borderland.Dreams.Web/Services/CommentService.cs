using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public class CommentService(ApplicationDbContext context, IMapperFacade mapper, IAppTime appTime) : ICommentService
{
    private const int MaxBodyLength = 1000;

    public async Task<IReadOnlyList<CommentViewModel>> GetForDream(int dreamId)
    {
        var comments = await context.Comment
            .AsNoTracking()
            .IgnoreAutoIncludes() // User auto includes wallets which are not needed here
            .Where(comment => comment.DreamID == dreamId)
            .Include(comment => comment.Author)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.ID)
            .ToListAsync();

        var viewModels = mapper.ToViewModels(comments);
        var viewModelsById = viewModels.ToDictionary(comment => comment.Id);
        var topLevelComments = new List<CommentViewModel>();

        // Organize comments by threads
        foreach (var comment in viewModels)
        {
            if (comment.ParentCommentId is not int parentCommentId)
            {
                topLevelComments.Add(comment);
                continue;
            }

            if (viewModelsById.TryGetValue(parentCommentId, out var parent))
                parent.Replies.Add(comment);
        }

        foreach (var comment in topLevelComments)
        {
            comment.IsReplyAllowed = !comment.IsDeleted;
            comment.Replies.RemoveAll(reply => reply.IsDeleted);
            foreach (var reply in comment.Replies)
                reply.IsReplyAllowed = comment.IsReplyAllowed;
        }

        topLevelComments.RemoveAll(comment => comment.IsDeleted && comment.Replies.Count == 0);
        return topLevelComments;
    }

    public Task<CommentServiceResult> Post(int dreamId, string authorId, string? body) =>
        Save(dreamId, authorId, body, parentCommentId: null);

    public Task<CommentServiceResult> Reply(
        int dreamId,
        int parentCommentId,
        string authorId,
        string? body) =>
        Save(dreamId, authorId, body, parentCommentId);

    private async Task<CommentServiceResult> Save(
        int dreamId,
        string authorId,
        string? body,
        int? parentCommentId)
    {
        var dream = await context.Dream
            .AsNoTracking()
            .Where(dream => dream.ID == dreamId)
            .Select(dream => new { dream.CloudID })
            .SingleOrDefaultAsync();

        if (dream == null)
            return new CommentServiceResult(CommentServiceStatus.DreamNotFound);

        if (!await IsCloudMemberAsync(dream.CloudID, authorId))
            return new CommentServiceResult(CommentServiceStatus.NotMember);

        var (trimmedBody, bodyError) = ValidateBody(body);
        if (bodyError != null)
            return new CommentServiceResult(CommentServiceStatus.InvalidBody, ErrorMessage: bodyError);

        // Serialize parent validation and insertion with comment deletion in SQLite.
        await using var transaction = parentCommentId.HasValue
            ? await context.Database.BeginTransactionAsync()
            : null;
        if (parentCommentId.HasValue)
        {
            var parent = await context.Comment
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Include(comment => comment.ParentComment)
                .SingleOrDefaultAsync(comment => comment.ID == parentCommentId.Value);

            if (parent == null || parent.DreamID != dreamId)
                return new CommentServiceResult(CommentServiceStatus.InvalidParent);

            if (parent.DeletedUtc.HasValue || parent.ParentComment?.DeletedUtc != null)
                return new CommentServiceResult(CommentServiceStatus.CommentDeleted);

            if (parent.ParentCommentID.HasValue)
            {
                if (parent.ParentComment == null
                    || parent.ParentComment.DreamID != dreamId
                    || parent.ParentComment.ParentCommentID.HasValue)
                {
                    return new CommentServiceResult(CommentServiceStatus.InvalidParent);
                }

                parentCommentId = parent.ParentComment.ID;
            }
        }

        var comment = new Comment
        {
            DreamID = dreamId,
            AuthorID = authorId,
            ParentCommentID = parentCommentId,
            Body = trimmedBody,
            CreatedAtUtc = appTime.UtcNow
        };
        context.Comment.Add(comment);
        await context.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        return new CommentServiceResult(CommentServiceStatus.Success, comment.ID);
    }

    public async Task<CommentServiceResult> Edit(int dreamId, int commentId, string authorId, string? body)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var comment = await context.Comment
            .Include(comment => comment.Dream)
            .SingleOrDefaultAsync(comment => comment.ID == commentId && comment.DreamID == dreamId);
        if (comment == null)
            return new CommentServiceResult(CommentServiceStatus.CommentNotFound);

        if (comment.AuthorID != authorId)
            return new CommentServiceResult(CommentServiceStatus.NotAuthor);

        if (comment.DeletedUtc.HasValue)
            return new CommentServiceResult(CommentServiceStatus.CommentDeleted);

        if (!await IsCloudMemberAsync(comment.Dream.CloudID, authorId))
            return new CommentServiceResult(CommentServiceStatus.NotMember);

        var (trimmedBody, bodyError) = ValidateBody(body);
        if (bodyError != null)
            return new CommentServiceResult(CommentServiceStatus.InvalidBody, ErrorMessage: bodyError);

        comment.Body = trimmedBody;
        comment.EditedUtc = appTime.UtcNow;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CommentServiceResult(CommentServiceStatus.Success, comment.ID);
    }

    public async Task<CommentServiceResult> Delete(int cloudId, int dreamId, int commentId, string authorId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var comment = await context.Comment
            .SingleOrDefaultAsync(comment => comment.ID == commentId
                && comment.DreamID == dreamId
                && comment.Dream.CloudID == cloudId);
        if (comment == null)
            return new CommentServiceResult(CommentServiceStatus.CommentNotFound);

        if (comment.AuthorID != authorId)
            return new CommentServiceResult(CommentServiceStatus.NotAuthor);

        if (!comment.DeletedUtc.HasValue)
        {
            comment.Body = string.Empty;
            comment.DeletedUtc = appTime.UtcNow;
            comment.DeletedByUserId = authorId;
            await context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return new CommentServiceResult(CommentServiceStatus.Success, comment.ID);
    }

    private Task<bool> IsCloudMemberAsync(int cloudId, string userId) =>
        context.UserClaims.AnyAsync(claim =>
            claim.UserId == userId &&
            claim.ClaimType == Constants.JOINED_CLOUD_CLAIM &&
            claim.ClaimValue == cloudId.ToString());

    private static (string Body, string? Error) ValidateBody(string? body)
    {
        var trimmedBody = body?.Trim() ?? string.Empty;
        if (trimmedBody.Length == 0)
            return (trimmedBody, "A comment is required.");

        if (trimmedBody.Length > MaxBodyLength)
            return (trimmedBody, "Comments must be 1000 characters or fewer.");

        return (trimmedBody, null);
    }
}
