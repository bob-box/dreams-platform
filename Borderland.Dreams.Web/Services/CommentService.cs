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

        var cloudId = dream.CloudID.ToString();
        var isCloudMember = await context.UserClaims.AnyAsync(claim =>
            claim.UserId == authorId &&
            claim.ClaimType == Constants.JOINED_CLOUD_CLAIM &&
            claim.ClaimValue == cloudId);

        if (!isCloudMember)
            return new CommentServiceResult(CommentServiceStatus.NotMember);

        var trimmedBody = body?.Trim() ?? string.Empty;
        if (trimmedBody.Length == 0)
        {
            return new CommentServiceResult(
                CommentServiceStatus.InvalidBody,
                ErrorMessage: "A comment is required.");
        }

        if (trimmedBody.Length > MaxBodyLength)
        {
            return new CommentServiceResult(
                CommentServiceStatus.InvalidBody,
                ErrorMessage: "Comments must be 1000 characters or fewer.");
        }

        if (parentCommentId.HasValue)
        {
            var parent = await context.Comment
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Include(comment => comment.ParentComment)
                .SingleOrDefaultAsync(comment => comment.ID == parentCommentId.Value);

            if (parent == null || parent.DreamID != dreamId)
                return new CommentServiceResult(CommentServiceStatus.InvalidParent);

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

        return new CommentServiceResult(CommentServiceStatus.Success, comment.ID);
    }
}
