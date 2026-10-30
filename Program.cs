using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Authorization.Dream;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Services;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Scheduling;
using Borderland.Dreams.FilterAttributes;
using Borderland.Dreams.Utils;
using Borderland.Dreams.Mappers;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.Extensions.Options;
using Ganss.Xss;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add Hangfire services.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage());
    
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSeeding((context, _) => DataSeeder.Seed((ApplicationDbContext)context))
            .UseAsyncSeeding((context, _, _) =>
                DataSeeder.SeedAsync(((ApplicationDbContext)context).ApplicationServiceProvider));
    }
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure time services and options.
builder.Services.Configure<TimeOptions>(builder.Configuration.GetSection("Time"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAppTime>(sp =>
{
    var timeProvider = sp.GetRequiredService<TimeProvider>();
    var options = sp.GetRequiredService<IOptions<TimeOptions>>().Value;

    return new AppTime(timeProvider, options);
});

// Application services.
builder.Services.AddScoped<IMapperFacade, MapperFacade>();
builder.Services.AddScoped<IDreamService, DreamService>();
builder.Services.AddScoped<IDreamFundingService, DreamFundingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICloudService, CloudService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFundingRoundService, FundingRoundService>();
builder.Services.AddScoped<TestDataService>();
builder.Services.AddScoped<ICloudNameResolver, CloudNameResolver>();
builder.Services.AddScoped<FundingRoundSchedulerService>();
builder.Services.AddScoped<FundingRoundOutboxDispatcher>();
builder.Services.AddScoped<PostmarkEmailSender>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(services =>
    services.GetRequiredService<PostmarkEmailSender>());
builder.Services.AddScoped<IPostmarkEmailService>(services =>
    services.GetRequiredService<PostmarkEmailSender>());
builder.Services.AddScoped<EmailCampaignService>();
builder.Services.AddScoped<IEmailCampaignManagementService, EmailCampaignManagementService>();
builder.Services.AddScoped<IMembershipCheckService, MembershipCheckService>();
builder.Services.AddSingleton<ICloudCache, CloudCache>();

builder.Services.AddSingleton<HtmlSanitizer>(_ => new HtmlSanitizer());

builder.Services.AddHangfireServer(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.WorkerCount = 1;
    }
});

// Authorization handlers.
// Services using Entity Framework must be registered for dependency injection using AddScoped.
// UserIsMainDreamer and UserIsCoDreamer uses ASP.NET Core Identity (UserManager), which is built on Entity Framework Core.
builder.Services.AddScoped<IAuthorizationHandler, UserIsMainDreamerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, UserIsCoDreamerAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, UserIsAdminAuthorizationHandler>();

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

// during dev
var logStore = new InMemoryLogStore();
builder.Services.AddSingleton(logStore);
builder.Logging.AddProvider(new InMemoryLoggerProvider(logStore));

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("sv-SE"),
    // Formatting numbers, dates, etc.
    SupportedCultures = [new("sv-SE")],
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Add development middleware that lets you apply pending EF Core migrations through the browser’s Database Error Page.
    // It does not run migrations automatically when the app starts; it provides a way to trigger them after a migration-related database error.
    app.UseMigrationsEndPoint();
    try
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database initialization failed.");
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// When to use the CloudNameResolverMiddleware. 
// Seems to always be needed when adding a new controller
app.UseWhen(context =>
{
    var controller = context.Request.RouteValues["controller"]?.ToString();
    var area = context.Request.RouteValues["area"]?.ToString();
    return new[] { "Dream", "Cloud", "Info" }.Contains(controller) || area == "Admin";
}, appBuilder =>
{
    // Custom middleware to resolve CloudName to CloudID. Must be placed after UseRouting to get route data.
    appBuilder.UseMiddleware<CloudNameResolverMiddleware>();
});

app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

// Using the outbox pattern for reliable messaging.
// This creates a job that checks the outbox every minute for actions to do.
RecurringJob.AddOrUpdate<FundingRoundOutboxDispatcher>(
    "funding-round-outbox-dispatcher",
    dispatcher => dispatcher.DispatchPendingAsync(),
    Cron.Minutely);

app.MapControllerRoute(
    name: "areas-within-a-cloud", // For example editing a cloud or its funding rounds
    pattern: "{cloudSlug:required}/{area:exists}/{controller:required}/{action=Index}/{id?}"
    );
app.MapControllerRoute(
    name: "areas-outside-of-cloud", // for example creating a new cloud
    pattern: "{area:exists}/{controller:required}/{action=Index}/{id?}"
    );
app.MapControllerRoute(
    name: "cloudSlug-dreams-group",
    pattern: "{cloudSlug:required}/{controller:required}/{action=Index}/{id?}"
    );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    );
app.MapRazorPages();

app.Run();