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

    public DbSet<Cloud> Cloud { get; set; } = default!;

    public DbSet<Dream> Dream { get; set; } = default!;

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dream>()
            .HasMany(e => e.Tags)
            .WithMany();

        modelBuilder.Entity<Cloud>()
            .HasIndex(e => e.Name)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
