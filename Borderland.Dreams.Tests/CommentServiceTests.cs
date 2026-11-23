using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Models;
using Borderland.Dreams.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Borderland.Dreams.Tests;

public class CommentServiceTests
{
    [Fact]
    public async Task PostRejectsUserWhoHasNotJoinedTheDreamCloud()
    {
        await using var database = await TestDatabase.Create(isMember: false);
        var service = CreateService(database);

        var result = await service.Post(database.DreamId, database.UserId, "A thought");

        Assert.Equal(CommentServiceStatus.NotMember, result.Status);
        Assert.Empty(await database.Context.Comment.ToListAsync());
    }

    [Fact]
    public async Task ReplyFlattensRepliesUnderTheTopLevelComment()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var topLevel = await service.Post(database.DreamId, database.UserId, "Top level");
        var firstReply = await service.Reply(
            database.DreamId,
            topLevel.CommentId!.Value,
            database.UserId,
            "First reply");

        var secondReply = await service.Reply(
            database.DreamId,
            firstReply.CommentId!.Value,
            database.UserId,
            "Reply to reply");
        var comments = await database.Context.Comment
            .OrderBy(comment => comment.ID)
            .ToListAsync();

        Assert.Equal(CommentServiceStatus.Success, secondReply.Status);
        Assert.Equal(topLevel.CommentId, comments[0].ID);
        Assert.Null(comments[0].ParentCommentID);
        Assert.Equal(topLevel.CommentId, comments[1].ParentCommentID);
        Assert.Equal(topLevel.CommentId, comments[2].ParentCommentID);
    }

    [Fact]
    public async Task ReplyRejectsParentFromAnotherDream()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var otherDream = new Dream
        {
            Name = "Other dream",
            CloudID = await database.Context.Dream
                .Where(dream => dream.ID == database.DreamId)
                .Select(dream => dream.CloudID)
                .SingleAsync()
        };
        database.Context.Dream.Add(otherDream);
        await database.Context.SaveChangesAsync();

        var service = CreateService(database);
        var parent = await service.Post(database.DreamId, database.UserId, "Parent");
        var result = await service.Reply(
            otherDream.ID,
            parent.CommentId!.Value,
            database.UserId,
            "Invalid reply");

        Assert.Equal(CommentServiceStatus.InvalidParent, result.Status);
        Assert.Equal(1, await database.Context.Comment.CountAsync());
    }

    [Fact]
    public async Task ReplyRejectsNonMember()
    {
        await using var database = await TestDatabase.Create(isMember: false);
        var parent = new Comment
        {
            DreamID = database.DreamId,
            AuthorID = database.UserId,
            Body = "Parent",
            CreatedAtUtc = database.AppTime.UtcNow
        };
        database.Context.Comment.Add(parent);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.Reply(
            database.DreamId,
            parent.ID,
            database.UserId,
            "Reply");

        Assert.Equal(CommentServiceStatus.NotMember, result.Status);
        Assert.Equal(1, await database.Context.Comment.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task PostRejectsInvalidBodies(string? body)
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);

        var result = await service.Post(database.DreamId, database.UserId, body);

        Assert.Equal(CommentServiceStatus.InvalidBody, result.Status);
        Assert.Empty(await database.Context.Comment.ToListAsync());
    }

    [Fact]
    public async Task PostRejectsBodiesLongerThan1000Characters()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);

        var result = await service.Post(database.DreamId, database.UserId, new string('x', 1001));

        Assert.Equal(CommentServiceStatus.InvalidBody, result.Status);
        Assert.Empty(await database.Context.Comment.ToListAsync());
    }

    [Fact]
    public async Task PostSavesTrimmedBodyWithUtcTimestamp()
    {
        await using var database = await TestDatabase.Create(isMember: true, applyMigrations: true);
        var service = CreateService(database);

        var result = await service.Post(database.DreamId, database.UserId, "  A thought  ");
        var comment = await database.Context.Comment.SingleAsync();

        Assert.Equal(CommentServiceStatus.Success, result.Status);
        Assert.Equal(result.CommentId, comment.ID);
        Assert.Equal(database.DreamId, comment.DreamID);
        Assert.Equal(database.UserId, comment.AuthorID);
        Assert.Equal("A thought", comment.Body);
        Assert.Equal(database.AppTime.UtcNow, comment.CreatedAtUtc);
    }

    [Fact]
    public async Task EditUpdatesOwnCommentAndSetsEditedTimestamp()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");
        database.AppTime.Advance(TimeSpan.FromMinutes(1));

        var result = await service.Edit(
            database.DreamId,
            posted.CommentId!.Value,
            database.UserId,
            "  Corrected comment  ");
        var comment = await database.Context.Comment.SingleAsync();
        var viewModel = Assert.Single(await service.GetForDream(database.DreamId));

        Assert.Equal(CommentServiceStatus.Success, result.Status);
        Assert.Equal("Corrected comment", comment.Body);
        Assert.Equal(database.AppTime.UtcNow, comment.EditedUtc);
        Assert.Equal(database.AppTime.UtcNow, viewModel.EditedAt);
    }

    [Fact]
    public async Task EditAllowsAuthorToEditAReply()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var parent = await service.Post(database.DreamId, database.UserId, "Parent");
        var reply = await service.Reply(
            database.DreamId,
            parent.CommentId!.Value,
            database.UserId,
            "Original reply");

        var result = await service.Edit(
            database.DreamId,
            reply.CommentId!.Value,
            database.UserId,
            "Edited reply");

        Assert.Equal(CommentServiceStatus.Success, result.Status);
        Assert.Equal("Edited reply", await database.Context.Comment
            .Where(comment => comment.ID == reply.CommentId)
            .Select(comment => comment.Body)
            .SingleAsync());
    }

    [Fact]
    public async Task EditRejectsAnotherAuthor()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");
        var otherUserId = await database.AddUserAsync("other-member-id", "Other member", isMember: true);

        var result = await service.Edit(
            database.DreamId,
            posted.CommentId!.Value,
            otherUserId,
            "Changed by someone else");
        var comment = await database.Context.Comment.SingleAsync();

        Assert.Equal(CommentServiceStatus.NotAuthor, result.Status);
        Assert.Equal("Original", comment.Body);
        Assert.Null(comment.EditedUtc);
    }

    [Fact]
    public async Task EditRejectsFormerMemberButCommentRemainsVisible()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");
        var membershipClaim = await database.Context.UserClaims.SingleAsync(claim =>
            claim.UserId == database.UserId &&
            claim.ClaimType == Constants.JOINED_CLOUD_CLAIM);
        database.Context.UserClaims.Remove(membershipClaim);
        await database.Context.SaveChangesAsync();

        var result = await service.Edit(
            database.DreamId,
            posted.CommentId!.Value,
            database.UserId,
            "Changed after leaving");
        var visibleComment = Assert.Single(await service.GetForDream(database.DreamId));

        Assert.Equal(CommentServiceStatus.NotMember, result.Status);
        Assert.Equal("Original", visibleComment.Body);
        Assert.Null(visibleComment.EditedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task EditRejectsEmptyBodies(string? body)
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");

        var result = await service.Edit(
            database.DreamId,
            posted.CommentId!.Value,
            database.UserId,
            body);

        Assert.Equal(CommentServiceStatus.InvalidBody, result.Status);
        Assert.Equal("Original", await database.Context.Comment
            .Where(comment => comment.ID == posted.CommentId)
            .Select(comment => comment.Body)
            .SingleAsync());
    }

    [Fact]
    public async Task EditRejectsBodiesLongerThan1000Characters()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");

        var result = await service.Edit(
            database.DreamId,
            posted.CommentId!.Value,
            database.UserId,
            new string('x', 1001));

        Assert.Equal(CommentServiceStatus.InvalidBody, result.Status);
        Assert.Equal("Original", await database.Context.Comment
            .Where(comment => comment.ID == posted.CommentId)
            .Select(comment => comment.Body)
            .SingleAsync());
    }

    [Fact]
    public async Task DeleteClearsBodyAndPreservesOriginalDeletionMetadataOnRetry()
    {
        await using var database = await TestDatabase.Create(isMember: true, applyMigrations: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Private text");
        var commentId = posted.CommentId!.Value;
        var deletedAt = database.AppTime.UtcNow;

        var result = await service.Delete(database.DreamId, commentId, database.UserId);
        database.AppTime.Advance(TimeSpan.FromMinutes(1));
        var repeated = await service.Delete(database.DreamId, commentId, database.UserId);

        Assert.Equal(CommentServiceStatus.Success, result.Status);
        Assert.Equal(CommentServiceStatus.Success, repeated.Status);
        var comment = await database.Context.Comment.AsNoTracking().SingleAsync();
        Assert.Equal(commentId, comment.ID);
        Assert.Empty(comment.Body);
        Assert.Equal(deletedAt, comment.DeletedUtc);
        Assert.Equal(database.UserId, comment.DeletedByUserId);
        var deletionAudit = await database.Context.AuditLog.SingleAsync(entry =>
            entry.EntityType == "Comment" && entry.EntityID == commentId && entry.Action == "Deleted");
        Assert.DoesNotContain("Private text", deletionAudit.Changes);
        Assert.Empty(await service.GetForDream(database.DreamId));
    }

    [Fact]
    public async Task DeleteAllowsFormerCloudMemberToDeleteOwnReply()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Parent");
        var reply = await service.Reply(database.DreamId, posted.CommentId!.Value, database.UserId, "Reply");
        database.Context.UserClaims.Remove(await database.Context.UserClaims.SingleAsync());
        await database.Context.SaveChangesAsync();

        var result = await service.Delete(database.DreamId, reply.CommentId!.Value, database.UserId);

        Assert.Equal(CommentServiceStatus.Success, result.Status);
        var parent = Assert.Single(await service.GetForDream(database.DreamId));
        Assert.Empty(parent.Replies);
    }

    [Fact]
    public async Task DeleteRejectsAnotherAuthorAndMismatchedDream()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");
        var otherUser = await database.AddUserAsync("other-id", "Other", isMember: true);
        var commentId = posted.CommentId!.Value;

        var otherAuthor = await service.Delete(database.DreamId, commentId, otherUser);
        var wrongDream = await service.Delete(database.DreamId + 1, commentId, database.UserId);

        Assert.Equal(CommentServiceStatus.NotAuthor, otherAuthor.Status);
        Assert.Equal(CommentServiceStatus.CommentNotFound, wrongDream.Status);
        var comment = await database.Context.Comment.AsNoTracking().SingleAsync();
        Assert.Equal("Original", comment.Body);
        Assert.Null(comment.DeletedUtc);
        Assert.Null(comment.DeletedByUserId);
    }

    [Fact]
    public async Task DeletedParentKeepsVisibleRepliesAndRejectsEditsAndNewReplies()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Parent");
        var commentId = posted.CommentId!.Value;
        var replyAuthor = await database.AddUserAsync("reply-author", "Reply author", isMember: true);
        var reply = await service.Reply(database.DreamId, commentId, replyAuthor, "Visible reply");
        var replyId = reply.CommentId!.Value;
        var deletedReply = await service.Reply(database.DreamId, commentId, database.UserId, "Hidden reply");
        await service.Delete(database.DreamId, deletedReply.CommentId!.Value, database.UserId);
        await service.Delete(database.DreamId, commentId, database.UserId);

        var parent = Assert.Single(await service.GetForDream(database.DreamId));
        Assert.True(parent.IsDeleted);
        Assert.Empty(parent.Body);
        Assert.Equal(replyId, Assert.Single(parent.Replies).Id);

        var edit = await service.Edit(database.DreamId, commentId, database.UserId, "Restore");
        var replyToDeleted = await service.Reply(database.DreamId, commentId, database.UserId, "New reply");
        var replyThroughChild = await service.Reply(database.DreamId, replyId, database.UserId, "New reply");
        Assert.Equal(CommentServiceStatus.CommentDeleted, edit.Status);
        Assert.Equal(CommentServiceStatus.CommentDeleted, replyToDeleted.Status);
        Assert.Equal(CommentServiceStatus.CommentDeleted, replyThroughChild.Status);
        Assert.Equal(3, await database.Context.Comment.CountAsync());

        await service.Delete(database.DreamId, replyId, replyAuthor);
        Assert.Empty(await service.GetForDream(database.DreamId));
    }

    [Fact]
    public async Task DeletedReplyRejectsEditsAndRepliesWhileItsParentRemainsOpen()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var parent = await service.Post(database.DreamId, database.UserId, "Parent");
        var parentId = parent.CommentId!.Value;
        var reply = await service.Reply(database.DreamId, parentId, database.UserId, "Reply to delete");
        var replyId = reply.CommentId!.Value;
        Assert.Equal(CommentServiceStatus.Success,
            (await service.Delete(database.DreamId, replyId, database.UserId)).Status);

        var edit = await service.Edit(database.DreamId, replyId, database.UserId, "Restore");
        var replyToDeleted = await service.Reply(database.DreamId, replyId, database.UserId, "Invalid reply");
        var replyToParent = await service.Reply(database.DreamId, parentId, database.UserId, "New reply");

        Assert.Equal(CommentServiceStatus.CommentDeleted, edit.Status);
        Assert.Equal(CommentServiceStatus.CommentDeleted, replyToDeleted.Status);
        Assert.Equal(CommentServiceStatus.Success, replyToParent.Status);
        var visibleParent = Assert.Single(await service.GetForDream(database.DreamId));
        Assert.Equal(replyToParent.CommentId, Assert.Single(visibleParent.Replies).Id);
    }

    [Fact]
    public async Task DeleteStillRejectsNonAuthorAfterCommentHasBeenDeleted()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var posted = await service.Post(database.DreamId, database.UserId, "Original");
        var commentId = posted.CommentId!.Value;
        var otherUser = await database.AddUserAsync("other-id", "Other", isMember: true);
        Assert.Equal(CommentServiceStatus.Success,
            (await service.Delete(database.DreamId, commentId, database.UserId)).Status);

        var result = await service.Delete(database.DreamId, commentId, otherUser);

        Assert.Equal(CommentServiceStatus.NotAuthor, result.Status);
        var comment = await database.Context.Comment.AsNoTracking().SingleAsync();
        Assert.Equal(database.UserId, comment.DeletedByUserId);
    }

    [Fact]
    public async Task MigrationPreservesExistingComments()
    {
        await using var database = await TestDatabase.Create(
            isMember: true,
            migrateLegacyComment: true);

        var comment = await database.Context.Comment.SingleAsync();

        Assert.Equal("Legacy comment", comment.Body);
        Assert.Equal(database.UserId, comment.AuthorID);
        Assert.Equal(database.AppTime.UtcNow, comment.CreatedAtUtc);
        Assert.Null(comment.EditedUtc);
        Assert.Null(comment.DeletedUtc);
        Assert.Null(comment.DeletedByUserId);
    }

    [Fact]
    public async Task GetForDreamLoadsCommentsInOneQuery()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        await service.Post(database.DreamId, database.UserId, "A thought");
        database.QueryCounter.Reset();

        var comments = await service.GetForDream(database.DreamId);

        Assert.Equal(1, database.QueryCounter.ReaderQueries);
        Assert.Equal("Member", Assert.Single(comments).AuthorName);
    }

    [Fact]
    public async Task GetForDreamGroupsRepliesUnderTheirParentsInOldestFirstOrder()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        var firstTopLevel = await service.Post(database.DreamId, database.UserId, "First top-level");
        database.AppTime.Advance(TimeSpan.FromMinutes(1));
        var secondTopLevel = await service.Post(database.DreamId, database.UserId, "Second top-level");
        database.AppTime.Advance(TimeSpan.FromMinutes(1));
        await service.Reply(database.DreamId, firstTopLevel.CommentId!.Value, database.UserId, "First reply");
        database.AppTime.Advance(TimeSpan.FromMinutes(1));
        await service.Reply(database.DreamId, secondTopLevel.CommentId!.Value, database.UserId, "Other parent reply");
        database.AppTime.Advance(TimeSpan.FromMinutes(1));
        await service.Reply(database.DreamId, firstTopLevel.CommentId.Value, database.UserId, "Second reply");
        database.QueryCounter.Reset();

        var comments = await service.GetForDream(database.DreamId);

        Assert.Equal(1, database.QueryCounter.ReaderQueries);
        Assert.Equal(["First top-level", "Second top-level"], comments.Select(comment => comment.Body));
        Assert.Equal(["First reply", "Second reply"], comments[0].Replies.Select(reply => reply.Body));
        Assert.Equal("Other parent reply", Assert.Single(comments[1].Replies).Body);
    }

    [Fact]
    public async Task GetForDreamKeepsCommentsAndUsesDeletedUserLabelWhenAuthorIsRemoved()
    {
        await using var database = await TestDatabase.Create(isMember: true);
        var service = CreateService(database);
        await service.Post(database.DreamId, database.UserId, "A thought");

        var author = await database.Context.Users.SingleAsync(user => user.Id == database.UserId);
        database.Context.Users.Remove(author);
        await database.Context.SaveChangesAsync();

        var comments = await service.GetForDream(database.DreamId);

        Assert.Equal("[deleted user]", Assert.Single(comments).AuthorName);
        Assert.Equal(1, await database.Context.Comment.CountAsync());
    }

    private static CommentService CreateService(TestDatabase database) =>
        new(database.Context, new MapperFacade(database.AppTime, new HtmlSanitizer()), database.AppTime);

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(
            SqliteConnection connection,
            ApplicationDbContext context,
            FixedAppTime appTime,
            QueryCounter queryCounter,
            int dreamId,
            string userId)
        {
            _connection = connection;
            Context = context;
            AppTime = appTime;
            QueryCounter = queryCounter;
            DreamId = dreamId;
            UserId = userId;
        }

        public ApplicationDbContext Context { get; }
        public FixedAppTime AppTime { get; }
        public QueryCounter QueryCounter { get; }
        public int DreamId { get; }
        public string UserId { get; }

        public async Task<string> AddUserAsync(string userId, string name, bool isMember)
        {
            var user = new User { Id = userId, Name = name, UserName = userId };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            if (isMember)
            {
                var cloudId = await Context.Dream
                    .Where(dream => dream.ID == DreamId)
                    .Select(dream => dream.CloudID)
                    .SingleAsync();
                Context.UserClaims.Add(new IdentityUserClaim<string>
                {
                    UserId = userId,
                    ClaimType = Constants.JOINED_CLOUD_CLAIM,
                    ClaimValue = cloudId.ToString()
                });
                await Context.SaveChangesAsync();
            }

            return userId;
        }

        public static async Task<TestDatabase> Create(
            bool isMember,
            bool applyMigrations = false,
            bool migrateLegacyComment = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var queryCounter = new QueryCounter();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(queryCounter)
                .Options;
            var appTime = new FixedAppTime(new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc));
            var context = new ApplicationDbContext(
                options,
                appTime,
                new HttpContextAccessor(),
                new ServiceCollection().BuildServiceProvider());
            if (migrateLegacyComment)
                await context.Database.MigrateAsync("20261117201839_RemoveTotalFundsRemaningCloud");
            else if (applyMigrations)
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();

            var cloud = new Cloud
            {
                Name = "Test cloud",
                Slug = "test-cloud",
                DreamWelcomeEmailSubject = string.Empty,
                DreamWelcomeEmailBody = string.Empty
            };
            var user = new User { Id = "member-id", Name = "Member", UserName = "member" };
            var dream = new Dream { Name = "Test dream", Cloud = cloud };
            context.Cloud.Add(cloud);
            context.Users.Add(user);
            context.Dream.Add(dream);
            await context.SaveChangesAsync();

            if (isMember)
            {
                context.UserClaims.Add(new IdentityUserClaim<string>
                {
                    UserId = user.Id,
                    ClaimType = Constants.JOINED_CLOUD_CLAIM,
                    ClaimValue = cloud.ID.ToString()
                });
                await context.SaveChangesAsync();
            }

            if (migrateLegacyComment)
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO Comment (DreamID, AuthorID, Body, CreatedAtUtc) VALUES ({dream.ID}, {user.Id}, {"Legacy comment"}, {appTime.UtcNow})");
                await context.Database.MigrateAsync();
            }

            return new TestDatabase(connection, context, appTime, queryCounter, dream.ID, user.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FixedAppTime(DateTime utcNow) : IAppTime
    {
        public DateTime UtcNow { get; private set; } = utcNow;
        public DateTime Now => UtcNow;
        public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
        public DateTime ToLocal(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => local;
        public DateTime? ToLocal(DateTime? utc) => utc;
        public DateTime? ToUtc(DateTime? local) => local;
    }

    public sealed class QueryCounter : DbCommandInterceptor
    {
        public int ReaderQueries { get; private set; }

        public void Reset() => ReaderQueries = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ReaderQueries++;
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderQueries++;
            return ValueTask.FromResult(result);
        }
    }
}
