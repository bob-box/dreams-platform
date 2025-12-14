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
    public DbSet<BudgetItem> BudgetItem { get; set; } = default!;
    public DbSet<Dreamer> Dreamer { get; set; } = default!;
    public DbSet<Wallet> Wallet { get; set; } = default!;

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Navigation(e => e.Wallets)
            .AutoInclude();

        modelBuilder.Entity<Dream>()
            .HasMany(e => e.Tags)
            .WithMany();

        modelBuilder.Entity<Cloud>()
            .HasIndex(e => e.Name)
            .IsUnique();

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => new { x.UserID, x.CloudID })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}