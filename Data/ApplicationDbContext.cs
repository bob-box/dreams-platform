using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Models;

namespace Borderland.Dreams.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Borderland.Dreams.Models.Cloud> Cloud { get; set; } = default!;

    public DbSet<Borderland.Dreams.Models.Dream> Dream { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Dream>()
        .HasMany(e => e.Tags)
        .WithMany();
}
}
