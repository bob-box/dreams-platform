using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Authorization.Dream;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Services;
using Borderland.Dreams.Middleware;
using Hangfire;
using Hangfire.Storage.SQLite;
using Borderland.Dreams.FilterAttributes;
using Borderland.Dreams.Utils;
using Microsoft.Extensions.Options;
using Borderland.Dreams.Mappers;

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
    options.UseSqlite(connectionString));
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
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFundingRoundService, FundingRoundService>();
builder.Services.AddScoped<ICloudNameResolver, CloudNameResolver>();
builder.Services.AddScoped<FundingRoundSchedulerService>();
builder.Services.AddScoped<IMembershipCheckService, MembershipCheckService>();
builder.Services.AddSingleton<ICloudCache, CloudCache>();

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
    // Migrate to latest version and seed database on startup in development environment.
    app.UseMigrationsEndPoint();
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetService<ApplicationDbContext>() ?? throw new Exception("Failed to get ApplicationDbContext from service provider.");
        await dbContext.Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(services);
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

// When to use the CloudNameResolverMiddleware:
app.UseWhen(context =>
{
    var controller = context.Request.RouteValues["controller"]?.ToString();
    var area = context.Request.RouteValues["area"]?.ToString();
    return new[] { "Dream", "Cloud" }.Contains(controller) || area == "Admin";
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

app.MapControllerRoute(
    name: "areas",
    pattern: "{cloudName:required}/{area:exists}/{controller:required}/{action=Index}/{id?}"
    );

app.MapControllerRoute(
    name: "cloudName-dreams-group",
    pattern: "{cloudName:required}/{controller:required}/{action=Index}/{id?}"
    );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    );
app.MapRazorPages();

app.Run();