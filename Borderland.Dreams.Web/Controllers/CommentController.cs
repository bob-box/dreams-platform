using Borderland.Dreams.Authorization;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Borderland.Dreams.Controllers;

[Authorize]
public class CommentController(
    UserManager<User> userManager,
    ICommentService commentService,
    ICloudNameResolver cloudResolver
) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(CommentPostingRateLimitPolicy.Name)]
    public async Task<IActionResult> PostComment(int dreamId, [Bind("Body")] CommentPostViewModel input)
    {
        var authorId = userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("Not logged in");
        var result = await commentService.Post(dreamId, authorId, input.Body);
        return result.Status switch
        {
            CommentServiceStatus.Success => RedirectToComment(dreamId, result.CommentId!.Value),
            CommentServiceStatus.DreamNotFound => NotFound(),
            CommentServiceStatus.NotMember => Forbid(),
            CommentServiceStatus.InvalidBody => ReturnInvalidComment(dreamId, input, result.ErrorMessage!),
            _ => BadRequest("Unable to post comment.")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(CommentPostingRateLimitPolicy.Name)]
    public async Task<IActionResult> ReplyToComment(
        int dreamId,
        int parentCommentId,
        [Bind("Body")] CommentPostViewModel input)
    {
        var authorId = userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("Not logged in");
        var result = await commentService.Reply(dreamId, parentCommentId, authorId, input.Body);
        return result.Status switch
        {
            CommentServiceStatus.Success => RedirectToComment(dreamId, result.CommentId!.Value),
            CommentServiceStatus.DreamNotFound => NotFound(),
            CommentServiceStatus.InvalidParent => NotFound(),
            CommentServiceStatus.CommentDeleted => Conflict("This thread is closed to new replies."),
            CommentServiceStatus.NotMember => Forbid(),
            CommentServiceStatus.InvalidBody => ReturnInvalidReply(
                dreamId,
                parentCommentId,
                input,
                result.ErrorMessage!),
            _ => BadRequest("Unable to post reply.")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int dreamId,
        int commentId,
        [Bind("Body")] CommentPostViewModel input)
    {
        var authorId = userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("Not logged in");
        var result = await commentService.Edit(dreamId, commentId, authorId, input.Body);
        return result.Status switch
        {
            CommentServiceStatus.Success => RedirectToComment(dreamId, result.CommentId!.Value),
            CommentServiceStatus.CommentNotFound => NotFound(),
            CommentServiceStatus.CommentDeleted => Conflict("This comment has been deleted."),
            CommentServiceStatus.NotAuthor => Forbid(),
            CommentServiceStatus.NotMember => Forbid(),
            CommentServiceStatus.InvalidBody => ReturnInvalidEdit(
                dreamId,
                commentId,
                input,
                result.ErrorMessage!),
            _ => BadRequest("Unable to edit comment.")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int dreamId, int commentId)
    {
        var actorId = userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("Not logged in");
        if (cloudResolver.CloudID <= 0)
            return NotFound();

        var isAdmin = User.IsInRole(Constants.AdminRole);
        var result = await commentService.Delete(cloudResolver.CloudID, dreamId, commentId, actorId, isAdmin);
        return result.Status switch
        {
            CommentServiceStatus.Success => Redirect($"{GetDreamDetailsUrl(dreamId)}#comments"),
            CommentServiceStatus.CommentNotFound => NotFound(),
            CommentServiceStatus.NotAuthor => StatusCode(StatusCodes.Status403Forbidden),
            _ => BadRequest("Unable to delete comment.")
        };
    }

    private RedirectResult RedirectToComment(int dreamId, int commentId) =>
        Redirect($"{GetDreamDetailsUrl(dreamId)}#comment-{commentId}");

    private RedirectResult ReturnInvalidComment(int dreamId, CommentPostViewModel input, string errorMessage)
    {
        TempData[CommentTempDataKeys.InvalidCommentBody] = input.Body?.Trim() ?? string.Empty;
        TempData[CommentTempDataKeys.InvalidCommentError] = errorMessage;
        return Redirect($"{GetDreamDetailsUrl(dreamId)}#comments");
    }

    private RedirectResult ReturnInvalidReply(
        int dreamId,
        int parentCommentId,
        CommentPostViewModel input,
        string errorMessage)
    {
        TempData[CommentTempDataKeys.InvalidReplyBody] = input.Body?.Trim() ?? string.Empty;
        TempData[CommentTempDataKeys.InvalidReplyError] = errorMessage;
        TempData[CommentTempDataKeys.InvalidReplyParent] = parentCommentId;
        return Redirect($"{GetDreamDetailsUrl(dreamId)}#comment-{parentCommentId}");
    }

    private RedirectResult ReturnInvalidEdit(
        int dreamId,
        int commentId,
        CommentPostViewModel input,
        string errorMessage)
    {
        TempData[CommentTempDataKeys.InvalidEditBody] = input.Body?.Trim() ?? string.Empty;
        TempData[CommentTempDataKeys.InvalidEditError] = errorMessage;
        TempData[CommentTempDataKeys.InvalidEditComment] = commentId;
        return Redirect($"{GetDreamDetailsUrl(dreamId)}#comment-{commentId}");
    }

    private string GetDreamDetailsUrl(int dreamId) =>
        Url.Action(nameof(DreamController.Details), "Dream", new { id = dreamId })
        ?? throw new InvalidOperationException("Unable to create the dream URL.");
}

internal static class CommentTempDataKeys
{
    internal const string InvalidCommentBody = "InvalidCommentBody";
    internal const string InvalidCommentError = "InvalidCommentError";
    internal const string InvalidReplyBody = "InvalidReplyBody";
    internal const string InvalidReplyError = "InvalidReplyError";
    internal const string InvalidReplyParent = "InvalidReplyParent";
    internal const string InvalidEditBody = "InvalidEditBody";
    internal const string InvalidEditError = "InvalidEditError";
    internal const string InvalidEditComment = "InvalidEditComment";
}
