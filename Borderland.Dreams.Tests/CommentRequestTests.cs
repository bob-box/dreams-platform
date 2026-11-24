using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Controllers;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Models;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Borderland.Dreams.Tests;

public class CommentRequestTests
{
    private const string RateLimitMessage = "Please wait a minute before posting another comment.";

    [Fact]
    public async Task DeleteUsesSignedInRoleAndShowsControlsToNonMemberAdmin()
    {
        var directory = Directory.CreateTempSubdirectory("dreams-comment-requests-");
        try
        {
            await using var factory = new WebApplicationFactory<CommentController>();
            await using var app = WithIsolatedStorage(factory, directory);

            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(scope.ServiceProvider);
            var admin = await db.Users.SingleAsync(user => user.Email == "admin@dreams.com");
            var member = await db.Users.SingleAsync(user => user.Email == "user@dreams.com");
            var dream = await db.Dream.Include(dream => dream.Cloud).SingleAsync();
            db.UserClaims.RemoveRange(db.UserClaims
                .Where(claim => claim.UserId == admin.Id && claim.ClaimType == Constants.JOINED_CLOUD_CLAIM));
            var comment = new Comment { DreamID = dream.ID, AuthorID = admin.Id, Body = "Admin comment" };
            var reply = new Comment { DreamID = dream.ID, AuthorID = member.Id, Body = "Member reply", ParentComment = comment };
            db.Comment.AddRange(comment, reply);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var dreamPath = $"/{dream.Cloud.Slug}/dream/details/{dream.ID}";
            var deletePath = $"/{dream.Cloud.Slug}/comment/delete";

            using var regularClient = await LogInAsync(app, member.Email!);
            var regularPage = await regularClient.GetStringAsync(dreamPath);
            Assert.Equal([reply.ID], DeleteControlIds(regularPage, deletePath));
            using var forbidden = await DeleteAsync(regularClient, deletePath, regularPage, dream.ID, comment.ID);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            Assert.Equal("Admin comment", await db.Comment.Where(c => c.ID == comment.ID).Select(c => c.Body).SingleAsync());

            using var adminClient = await LogInAsync(app, admin.Email!);
            var adminPage = await adminClient.GetStringAsync(dreamPath);
            Assert.Equal([comment.ID, reply.ID], DeleteControlIds(adminPage, deletePath));
            using var deleted = await DeleteAsync(adminClient, deletePath, adminPage, dream.ID, reply.ID);
            Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
            var storedReply = await db.Comment.SingleAsync(c => c.ID == reply.ID);
            Assert.Empty(storedReply.Body);
            Assert.Equal(admin.Id, storedReply.DeletedByUserId);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CommentAndReplyShareTheAccountLimitAcrossCloudsAndLogins()
    {
        var directory = Directory.CreateTempSubdirectory("dreams-comment-requests-");
        try
        {
            await using var factory = new WebApplicationFactory<CommentController>();
            await using var app = WithIsolatedStorage(factory, directory);
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(scope.ServiceProvider);

            var member = await db.Users.SingleAsync(user => user.Email == "user@dreams.com");
            var dream = await db.Dream.Include(dream => dream.Cloud).SingleAsync();
            var otherCloud = new Cloud { Name = "Another Cloud", Slug = "another-cloud" };
            var otherDream = new Dream
            {
                Name = "Another dream",
                Status = DreamStatus.Active,
                Cloud = otherCloud,
                Dreamers = [new Dreamer { UserID = member.Id, IsMainDreamer = true }]
            };
            var existing = new Comment { DreamID = dream.ID, AuthorID = member.Id, Body = "Existing comment" };
            db.AddRange(otherDream, existing);
            await db.SaveChangesAsync();
            Assert.True((await users.AddClaimAsync(member, new Claim(Constants.JOINED_CLOUD_CLAIM, otherCloud.ID.ToString()))).Succeeded);

            var otherMember = new User
            {
                Email = "other@dreams.com", UserName = "other@dreams.com", Name = "Other member", EmailConfirmed = true
            };
            Assert.True((await users.CreateAsync(otherMember, "password1")).Succeeded);
            Assert.True((await users.AddClaimAsync(otherMember, new Claim(Constants.JOINED_CLOUD_CLAIM, dream.CloudID.ToString()))).Succeeded);

            var dreamPath = $"/{dream.Cloud.Slug}/dream/details/{dream.ID}";
            var commentPath = $"/{dream.Cloud.Slug}/comment";
            var otherDreamPath = $"/{otherCloud.Slug}/dream/details/{otherDream.ID}";
            using var client = await LogInAsync(app, member.Email!);
            using var secondLogin = await LogInAsync(app, member.Email!);
            using var otherMemberClient = await LogInAsync(app, otherMember.Email!);
            var page = await client.GetStringAsync(dreamPath);
            var secondLoginPage = await secondLogin.GetStringAsync(otherDreamPath);
            var otherMemberPage = await otherMemberClient.GetStringAsync(dreamPath);

            await EditAndDeleteAsync(client, commentPath, page, dream.ID, existing.ID);
            using var posted = await SubmitAsync(client, Form(page, "commentForm"), "First accepted comment");
            Assert.Equal(HttpStatusCode.Redirect, posted.StatusCode);
            var post = await db.Comment.SingleAsync(comment => comment.Body == "First accepted comment");
            Assert.Equal($"{dreamPath}#comment-{post.ID}", posted.Headers.Location?.OriginalString);

            page = await client.GetStringAsync(dreamPath);
            var replyForm = Form(page, $"reply-form-{post.ID}");
            using var replied = await SubmitAsync(client, replyForm, "Second accepted reply");
            Assert.Equal(HttpStatusCode.Redirect, replied.StatusCode);
            var reply = await db.Comment.SingleAsync(comment => comment.Body == "Second accepted reply");
            Assert.Equal(post.ID, reply.ParentCommentID);

            page = await client.GetStringAsync(dreamPath);
            replyForm = Form(page, $"reply-form-{reply.ID}");
            const string replyDraft = "Reply <draft & text>\nSecond line";
            using var rejectedReply = await SubmitAsync(client, replyForm, replyDraft);
            Assert.Equal(HttpStatusCode.Redirect, rejectedReply.StatusCode);
            Assert.Equal($"{dreamPath}#comment-{reply.ID}", rejectedReply.Headers.Location?.OriginalString);
            AssertNoDraft(await client.GetStringAsync(otherDreamPath));
            var returnedPage = await client.GetStringAsync(rejectedReply.Headers.Location);
            AssertDraft(Form(returnedPage, $"reply-form-{reply.ID}"), replyDraft);
            Assert.Contains($"id=\"reply-form-container-{reply.ID}\" data-comment-form-container>", returnedPage);
            Assert.DoesNotContain(RateLimitMessage, Form(returnedPage, $"reply-form-{post.ID}"));
            AssertNoDraft(await client.GetStringAsync(dreamPath));

            const string commentDraft = "Other Cloud <draft & text>\nSecond line";
            using var rejectedComment = await SubmitAsync(secondLogin, Form(secondLoginPage, "commentForm"), commentDraft);
            Assert.Equal(HttpStatusCode.Redirect, rejectedComment.StatusCode);
            Assert.Equal($"{otherDreamPath}#comments", rejectedComment.Headers.Location?.OriginalString);
            AssertNoDraft(await secondLogin.GetStringAsync(dreamPath));
            AssertDraft(Form(await secondLogin.GetStringAsync(rejectedComment.Headers.Location), "commentForm"), commentDraft);
            AssertNoDraft(await secondLogin.GetStringAsync(otherDreamPath));

            using var invalidToken = await SubmitAsync(client, replyForm, "Invalid token", token: "invalid");
            Assert.Equal(HttpStatusCode.BadRequest, invalidToken.StatusCode);
            await EditAndDeleteAsync(client, commentPath, page, dream.ID, reply.ID);

            using var otherMemberPost = await SubmitAsync(otherMemberClient, Form(otherMemberPage, "commentForm"), "Independent post");
            Assert.Equal(HttpStatusCode.Redirect, otherMemberPost.StatusCode);
            Assert.True(await db.Comment.AnyAsync(comment => comment.AuthorID == otherMember.Id && comment.Body == "Independent post"));
            Assert.False(await db.Comment.AnyAsync(comment => comment.Body == replyDraft || comment.Body == commentDraft || comment.Body == "Invalid token"));
            Assert.True(await db.Comment.AnyAsync(comment => comment.ID == reply.ID && comment.EditedUtc != null && comment.DeletedUtc != null));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task AdminPostingIsUnlimitedAndStillRequiresSignInAndMembership()
    {
        var directory = Directory.CreateTempSubdirectory("dreams-comment-requests-");
        try
        {
            await using var factory = new WebApplicationFactory<CommentController>();
            await using var app = WithIsolatedStorage(factory, directory);
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(scope.ServiceProvider);
            var admin = await db.Users.SingleAsync(user => user.Email == "admin@dreams.com");
            var dream = await db.Dream.Include(dream => dream.Cloud).SingleAsync();
            var dreamPath = $"/{dream.Cloud.Slug}/dream/details/{dream.ID}";

            using var joinedAdmin = await LogInAsync(app, admin.Email!);
            var form = Form(await joinedAdmin.GetStringAsync(dreamPath), "commentForm");
            for (var i = 0; i < 3; i++)
            {
                using var posted = await SubmitAsync(joinedAdmin, form, $"Admin comment {i}");
                Assert.Equal(HttpStatusCode.Redirect, posted.StatusCode);
            }
            Assert.Equal(3, await db.Comment.CountAsync(comment => comment.AuthorID == admin.Id));

            using var anonymous = CreateClient(app);
            using var challenged = await SubmitAsync(anonymous, form, "Anonymous comment");
            Assert.Equal(HttpStatusCode.Redirect, challenged.StatusCode);
            Assert.Equal("/Identity/Account/Login", challenged.Headers.Location?.AbsolutePath, ignoreCase: true);

            db.UserClaims.RemoveRange(db.UserClaims
                .Where(claim => claim.UserId == admin.Id && claim.ClaimType == Constants.JOINED_CLOUD_CLAIM));
            await db.SaveChangesAsync();
            using var nonmemberAdmin = await LogInAsync(app, admin.Email!);
            var token = AntiforgeryToken(await nonmemberAdmin.GetStringAsync(dreamPath));
            using var forbidden = await SubmitAsync(nonmemberAdmin, form, "Nonmember admin comment", token);
            Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
            Assert.Equal("/Identity/Account/AccessDenied", forbidden.Headers.Location?.AbsolutePath, ignoreCase: true);
            Assert.Equal(3, await db.Comment.CountAsync());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static WebApplicationFactory<CommentController> WithIsolatedStorage(
        WebApplicationFactory<CommentController> factory, DirectoryInfo directory) => factory.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={Path.Combine(directory.FullName, "dreams.db")};Pooling=False"));
            services.RemoveAll<JobStorage>();
            services.AddSingleton<JobStorage>(_ => new SQLiteStorage(
                Path.Combine(directory.FullName, "hangfire.db"),
                new SQLiteStorageOptions { PoolSize = 0 }));
        });
    });

    private static HttpClient CreateClient(WebApplicationFactory<CommentController> app) => app.CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    private static async Task<HttpClient> LogInAsync(WebApplicationFactory<CommentController> app, string email)
    {
        var client = CreateClient(app);
        const string path = "/Identity/Account/Login";
        var page = await client.GetStringAsync(path);
        using var login = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = "password1",
            ["__RequestVerificationToken"] = AntiforgeryToken(page)
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private static async Task EditAndDeleteAsync(HttpClient client, string commentPath, string page, int dreamId, int commentId)
    {
        using var edited = await client.PostAsync($"{commentPath}/edit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["dreamId"] = dreamId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["Body"] = "Edited comment",
            ["__RequestVerificationToken"] = AntiforgeryToken(page)
        }));
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        using var deleted = await DeleteAsync(client, $"{commentPath}/delete", page, dreamId, commentId);
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
    }

    private static Task<HttpResponseMessage> SubmitAsync(HttpClient client, string form, string body, string? token = null)
    {
        var action = Regex.Match(form, """\baction="([^"]+)\x22""");
        Assert.True(action.Success, "Expected a form action.");
        return client.PostAsync(WebUtility.HtmlDecode(action.Groups[1].Value), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Body"] = body,
            ["__RequestVerificationToken"] = token ?? AntiforgeryToken(form)
        }));
    }

    private static string Form(string page, string id)
    {
        var form = Regex.Match(page, $$"""<form\b[^>]*\bid="{{Regex.Escape(id)}}"[^>]*>.*?</form>""", RegexOptions.Singleline);
        Assert.True(form.Success, $"Expected form {id}.");
        return form.Value;
    }

    private static void AssertDraft(string form, string draft)
    {
        Assert.Contains(RateLimitMessage, form);
        Assert.Contains("&lt;draft &amp; text&gt;", form);
        var textarea = Regex.Match(form, """<textarea\b[^>]*>(.*?)</textarea>""", RegexOptions.Singleline);
        Assert.True(textarea.Success, "Expected a draft textarea.");
        Assert.Equal(draft, WebUtility.HtmlDecode(textarea.Groups[1].Value).TrimStart('\r', '\n'));
    }

    private static void AssertNoDraft(string page)
    {
        Assert.DoesNotContain(RateLimitMessage, page);
        Assert.DoesNotContain("&lt;draft &amp; text&gt;", page);
    }

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string path, string page, int dreamId, int commentId) =>
        client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["dreamId"] = dreamId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["__RequestVerificationToken"] = AntiforgeryToken(page)
        }));

    private static string AntiforgeryToken(string page)
    {
        var input = Regex.Match(page, """name="__RequestVerificationToken"[^>]*value="([^"]+)\x22""");
        Assert.True(input.Success, "Expected an antiforgery token in the page.");
        return WebUtility.HtmlDecode(input.Groups[1].Value);
    }

    private static int[] DeleteControlIds(string page, string path) =>
        Regex.Matches(page, $$"""<form\b[^>]*\baction="{{Regex.Escape(path)}}(?:[/?][^"]*)?"[^>]*>(?:(?!</form>).)*?name="commentId"\s+value="(\d+)\x22""",
                RegexOptions.Singleline)
            .Select(form => int.Parse(form.Groups[1].Value))
            .ToArray();
}
