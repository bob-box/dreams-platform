using System.Net;
using System.Text.RegularExpressions;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Controllers;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Borderland.Dreams.Tests;

public class CommentDeletionRequestTests
{
    [Fact]
    public async Task DeleteUsesSignedInRoleAndShowsControlsToNonMemberAdmin()
    {
        var directory = Directory.CreateTempSubdirectory("dreams-comment-requests-");
        try
        {
            await using var factory = new WebApplicationFactory<CommentController>();
            await using var app = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite($"Data Source={Path.Combine(directory.FullName, "dreams.db")};Pooling=False"));
                    services.RemoveAll<JobStorage>();
                    services.AddSingleton<JobStorage>(_ =>
                        new SQLiteStorage(Path.Combine(directory.FullName, "hangfire.db")));
                });
            });

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

    private static async Task<HttpClient> LogInAsync(WebApplicationFactory<CommentController> app, string email)
    {
        var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
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
