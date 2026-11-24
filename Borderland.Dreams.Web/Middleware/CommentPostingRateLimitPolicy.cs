using System.Security.Claims;
using System.Threading.RateLimiting;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Controllers;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Borderland.Dreams.Middleware;

internal sealed class CommentPostingRateLimitPolicy(
    IOptions<IdentityOptions> identityOptions,
    IAntiforgery antiforgery,
    ITempDataDictionaryFactory tempDataFactory,
    LinkGenerator links) : IRateLimiterPolicy<string>
{
    public const string Name = "CommentPosting";
    private const string RejectionMessage = "Please wait a minute before posting another comment.";

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var user = context.User;
        var userId = user.FindFirstValue(identityOptions.Value.ClaimsIdentity.UserIdClaimType);
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(userId)
            || user.IsInRole(Constants.AdminRole))
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        return RateLimitPartition.Get(userId, static _ => new SlidingWindowRateLimiter(new()
        {
            PermitLimit = 2,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 60,
            AutoReplenishment = true
        }));
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => RejectAsync;

    private async ValueTask RejectAsync(OnRejectedContext rejection, CancellationToken cancellationToken)
    {
        var context = rejection.HttpContext;
        var request = context.Request;
        var response = context.Response;

        // The rate limiter runs before MVC's antiforgery filter and TempData save filter.
        if (!await antiforgery.IsRequestValidAsync(context))
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var form = request.HasFormContentType ? await request.ReadFormAsync(cancellationToken) : null;
        if (!TryGetId(form, request, "dreamId", out var dreamId))
        {
            response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var dreamPath = links.GetPathByAction(context, nameof(DreamController.Details), "Dream", new { id = dreamId })
            ?? throw new InvalidOperationException("Unable to create the dream URL.");
        var body = form?["Body"].FirstOrDefault() ?? string.Empty;
        if (body.Length > CommentPostViewModel.MaxBodyLength)
            body = body[..CommentPostViewModel.MaxBodyLength];

        var tempData = tempDataFactory.GetTempData(context);
        if (request.RouteValues["action"] as string == nameof(CommentController.ReplyToComment))
        {
            if (!TryGetId(form, request, "parentCommentId", out var parentId))
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            tempData[CommentTempDataKeys.InvalidReplyBody] = body;
            tempData[CommentTempDataKeys.InvalidReplyError] = RejectionMessage;
            tempData[CommentTempDataKeys.InvalidReplyParent] = parentId;
            response.Redirect($"{dreamPath}#comment-{parentId}");
        }
        else
        {
            tempData[CommentTempDataKeys.InvalidCommentBody] = body;
            tempData[CommentTempDataKeys.InvalidCommentError] = RejectionMessage;
            response.Redirect($"{dreamPath}#comments");
        }

        tempData.Save();
    }

    private static bool TryGetId(IFormCollection? form, HttpRequest request, string key, out int id) =>
        int.TryParse(form?[key].FirstOrDefault() ?? request.Query[key].FirstOrDefault(), out id) && id > 0;
}
