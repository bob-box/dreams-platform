using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Authorization.Dream;
using Borderland.Dreams.Services;
using Borderland.Dreams.Middleware;
using Hangfire;
using Hangfire.SQLite;
using Borderland.Dreams.FilterAttributes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

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

// Application services.
builder.Services.AddScoped<DreamService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ICloudNameResolver, CloudNameResolver>();
builder.Services.AddScoped<FundingRoundSchedulerService>();
builder.Services.AddSingleton<ICloudCache, CloudCache>();

// Add Hangfire services.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(connectionString));

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
    pattern: "{cloudName:required}/{area:exists}/{controller=Manage}/{action=Index}/{id?}"
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