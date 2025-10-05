using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Data;
using Borderland.Dreams.Models;
using Microsoft.AspNetCore.Authorization;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Authorization.Dream;
using Borderland.Dreams.Services;

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
        var dbContext = services.GetService<ApplicationDbContext>();
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

app.UseAuthorization();

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
