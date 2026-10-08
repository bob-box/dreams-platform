using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Models;
using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly IAppTime _appTime;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IAppTime appTime)
        : base(options)
    {
        _appTime = appTime;
        ValidateCreatedAtInterfaces();
    }

    public DbSet<Cloud> Cloud { get; set; } = default!;
    public DbSet<Dream> Dream { get; set; } = default!;
    public DbSet<BudgetItem> BudgetItem { get; set; } = default!;
    public DbSet<Dreamer> Dreamer { get; set; } = default!;
    public DbSet<Wallet> Wallet { get; set; } = default!;
    public DbSet<FundingRound> FundingRound { get; set; } = default!;
    public DbSet<Fund> Fund { get; set; } = default!;
    public DbSet<Tag> Tag { get; set; } = default!;
    public DbSet<CloudInfo> CloudInfo { get; set; } = default!;

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filters for soft delete
        modelBuilder.Entity<Cloud>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Dream>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<CloudInfo>().HasQueryFilter(x => !x.Cloud.IsDeleted);

        modelBuilder.Entity<User>()
            .Ignore(u => u.PhoneNumber)
            .Ignore(u => u.PhoneNumberConfirmed);

        modelBuilder.Entity<User>()
            .Navigation(e => e.Wallets)
            .AutoInclude();

        modelBuilder.Entity<Dream>()
            .HasMany(e => e.Tags)
            .WithMany();

        modelBuilder.Entity<Tag>()
            .HasOne(t => t.Cloud)
            .WithMany(c => c.Tags)
            .HasForeignKey(t => t.CloudID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cloud>()
            .HasIndex(e => e.Name)
            .IsUnique();

        modelBuilder.Entity<Cloud>()
            .HasIndex(e => e.Slug)
            .IsUnique();

        modelBuilder.Entity<CloudInfo>()
            .HasOne(e => e.Cloud)
            .WithOne(e => e.Info)
            .HasForeignKey<CloudInfo>(e => e.CloudID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => new { x.UserID, x.CloudID })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }

    private void ValidateCreatedAtInterfaces()
    {
        var entityTypes = Model.GetEntityTypes();

        var violations = entityTypes
            .Where(e => !e.ClrType.IsAssignableTo(typeof(IAutoPopulateCreatedAtOnAdd))
                     && e.GetProperties().Any(p => p.Name == nameof(IAutoPopulateCreatedAtOnAdd.CreatedAt)));

        if (violations.Any())
        {
            var names = string.Join(", ", violations.Select(e => e.ClrType.Name));
            throw new InvalidOperationException(
                $"The following entities have a CreatedAt property but are missing IAutoPopulateCreatedAtOnAdd: {names}");
        }
    }
    public override int SaveChanges()
    {
        ApplyCreatedAt();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyCreatedAt();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sets CreatedAt to current time for all newly added entities that implement IAutoPopulateCreatedAtOnAdd.
    /// </summary>
    private void ApplyCreatedAt()
    {
        var entries = ChangeTracker
            .Entries<IAutoPopulateCreatedAtOnAdd>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.CreatedAt = _appTime.UtcNow;
        }

        // Prevent modification of CreatedAt on existing entities
        var modifiedEntries = ChangeTracker
            .Entries<IAutoPopulateCreatedAtOnAdd>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            var property = entry.Property(e => e.CreatedAt);
            if (property.IsModified)
            {
                property.CurrentValue = property.OriginalValue;
                property.IsModified = false;
            }
        }
    }
}