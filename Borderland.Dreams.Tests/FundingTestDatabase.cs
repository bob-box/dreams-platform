using Borderland.Dreams.Authorization;
using Borderland.Dreams.Authorization.Dream;
using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.Scheduling;
using Borderland.Dreams.Services;
using Ganss.Xss;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Borderland.Dreams.Tests;

internal sealed class FundingTestDatabase : IAsyncDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("dreams-ci-");
    private readonly ServiceProvider services;

    public TestClock Clock { get; } = new();
    public MembershipStub Membership { get; } = new();

    public FundingTestDatabase(params IInterceptor[] interceptors)
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddOptions();
        registrations.AddHttpContextAccessor();
        registrations.AddAuthorizationCore();
        registrations.AddAuthentication();

        registrations.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(directory.FullName, "dreams.db")};Pooling=False")
                .AddInterceptors(interceptors));
        registrations.AddIdentityCore<User>().AddSignInManager().AddEntityFrameworkStores<ApplicationDbContext>();
        registrations.AddScoped<IAuthorizationHandler, UserIsMainDreamerAuthorizationHandler>();
        registrations.AddScoped<IAuthorizationHandler, UserIsCoDreamerAuthorizationHandler>();
        registrations.AddSingleton<IAuthorizationHandler, UserIsAdminAuthorizationHandler>();
        registrations.AddSingleton<IAppTime>(Clock);
        registrations.AddSingleton<IMembershipCheckService>(Membership);
        registrations.AddSingleton<IResendEmailService, UnusedEmailSender>();
        registrations.AddScoped<ICloudNameResolver, CloudNameResolver>();
        registrations.AddScoped<IDreamFundingService, DreamFundingService>();
        registrations.AddScoped<IDreamService, DreamService>();
        registrations.AddScoped<ICommentService, CommentService>();
        registrations.AddScoped<IFundingRoundService, FundingRoundService>();
        registrations.AddScoped<ICloudService, CloudService>();
        registrations.AddScoped<ITagService, TagService>();
        registrations.AddScoped<IUserService, UserService>();
        registrations.AddScoped<FundingRoundSchedulerService>();
        registrations.AddSingleton<IBackgroundJobClient, UnusedBackgroundJobClient>();
        registrations.AddMemoryCache();
        registrations.AddSingleton<ICloudCache, CloudCache>();
        registrations.AddSingleton<HtmlSanitizer>();
        registrations.AddScoped<IMapperFacade, MapperFacade>();
        services = registrations.BuildServiceProvider();
    }

    public AsyncServiceScope CreateScope(int cloudId = 1)
    {
        var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICloudNameResolver>().CloudID = cloudId;
        return scope;
    }

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        directory.Delete(recursive: true);
    }

    internal sealed class TestClock : IAppTime
    {
        public DateTime UtcNow { get; set; } = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
        public DateTime Now => UtcNow;
        public DateTime ToLocal(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => local;
        public DateTime? ToLocal(DateTime? utc) => utc;
        public DateTime? ToUtc(DateTime? local) => local;
    }

    internal sealed class MembershipStub : IMembershipCheckService
    {
        public Func<Task<bool>> Check { get; set; } = () => Task.FromResult(true);
        public Task<bool> IsActiveMember(string email) => Check();
    }

    private sealed class UnusedEmailSender : IResendEmailService
    {
        public Task SendEmailAsync(string to, string subject, string htmlBody, string? tag = null) =>
            throw new InvalidOperationException("Unexpected email during a funding test.");

        public Task SendEmailInBatchesAsync(IReadOnlyCollection<OutboundEmail> emails, string? tag = null) =>
            throw new InvalidOperationException("Unexpected email during a funding test.");
    }

    private sealed class UnusedBackgroundJobClient : IBackgroundJobClient
    {
        public string Create(Hangfire.Common.Job job, Hangfire.States.IState state) =>
            throw new InvalidOperationException("Unexpected Hangfire job.");

        public bool ChangeState(string jobId, Hangfire.States.IState state, string expectedState) =>
            throw new InvalidOperationException("Unexpected Hangfire state change.");
    }
}
