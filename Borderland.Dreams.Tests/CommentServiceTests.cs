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
    public async Task MigrationPreservesExistingComments()
    {
        await using var database = await TestDatabase.Create(
            isMember: true,
            migrateLegacyComment: true);

        var comment = await database.Context.Comment.SingleAsync();

        Assert.Equal("Legacy comment", comment.Body);
        Assert.Equal(database.UserId, comment.AuthorID);
        Assert.Equal(database.AppTime.UtcNow, comment.CreatedAtUtc);
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
                await context.Database.MigrateAsync("20261106083011_AddUtcToDateFieldNames");
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
                var legacyBody = "Legacy comment";
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO Comment (CreatedAtUtc, DreamID, Text, UserID) VALUES ({appTime.UtcNow}, {dream.ID}, {legacyBody}, {user.Id})");
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
        public DateTime UtcNow { get; } = utcNow;
        public DateTime Now => UtcNow;
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
