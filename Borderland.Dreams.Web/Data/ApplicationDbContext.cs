using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Borderland.Dreams.Models;
using Borderland.Dreams.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace Borderland.Dreams.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly IAppTime _appTime;
    private readonly IHttpContextAccessor _httpContextAccessor;
    internal IServiceProvider ApplicationServiceProvider { get; }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IAppTime appTime,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider applicationServiceProvider)
        : base(options)
    {
        _appTime = appTime;
        _httpContextAccessor = httpContextAccessor;
        ApplicationServiceProvider = applicationServiceProvider;
        ValidateCreatedAtInterfaces();
    }

    public DbSet<Cloud> Cloud { get; set; } = default!;
    public DbSet<Dream> Dream { get; set; } = default!;
    public DbSet<Comment> Comment { get; set; } = default!;
    public DbSet<BudgetItem> BudgetItem { get; set; } = default!;
    public DbSet<Dreamer> Dreamer { get; set; } = default!;
    public DbSet<Wallet> Wallet { get; set; } = default!;
    public DbSet<FundingRound> FundingRound { get; set; } = default!;
    public DbSet<Fund> Fund { get; set; } = default!;
    public DbSet<Tag> Tag { get; set; } = default!;
    public DbSet<CloudInfo> CloudInfo { get; set; } = default!;
    public DbSet<AuditLogEntry> AuditLog { get; set; } = default!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = default!;
    public DbSet<Email> Email { get; set; } = default!;

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

        modelBuilder.Entity<Cloud>()
            .Property(cloud => cloud.SendDreamWelcomeEmail)
            .HasDefaultValue(false);
            
        modelBuilder.Entity<CloudInfo>()
            .HasOne(e => e.Cloud)
            .WithOne(e => e.Info)
            .HasForeignKey<CloudInfo>(e => e.CloudID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => new { x.UserID, x.CloudID })
            .IsUnique();

        modelBuilder.Entity<AuditLogEntry>()
            .HasIndex(x => new { x.CloudID, x.CreatedAtUtc });
        modelBuilder.Entity<AuditLogEntry>()
            .HasIndex(x => new { x.EntityType, x.EntityID, x.Action });

        modelBuilder.Entity<Comment>()
            .Property(comment => comment.Body)
            .HasMaxLength(1000);
        modelBuilder.Entity<Comment>()
            .HasIndex(comment => new { comment.DreamID, comment.CreatedAtUtc });
        modelBuilder.Entity<Comment>()
            .HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorID)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Comment>()
            .HasOne(comment => comment.ParentComment)
            .WithMany(comment => comment.Replies)
            .HasForeignKey(comment => comment.ParentCommentID)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Ensures every entity with a <c>CreatedAtUtc</c> property has the interface for auto populating that field.
    /// </summary>
    private void ValidateCreatedAtInterfaces()
    {
        var entityTypes = Model.GetEntityTypes();

        var violations = entityTypes
            .Where(e => !e.ClrType.IsAssignableTo(typeof(IAutoPopulateCreatedAtOnAdd))
                     && e.GetProperties().Any(p => p.Name == nameof(IAutoPopulateCreatedAtOnAdd.CreatedAtUtc)));

        if (violations.Any())
        {
            var names = string.Join(", ", violations.Select(e => e.ClrType.Name));
            throw new InvalidOperationException(
                $"The following entities have a CreatedAt property but are missing IAutoPopulateCreatedAtOnAdd: {names}");
        }
    }

    /// <summary>
    /// Applies creation timestamps and saves tracked entity changes, recording their audit entries when needed.
    /// </summary>
    /// <returns>The total number of state entries written, including audit entries.</returns>
    /// <remarks>
    /// Entity changes are saved first so generated keys are available to the audit rows. For relational providers,
    /// both saves use the current transaction or a new transaction, preventing a failed audit write from leaving
    /// unaudited entity changes. If there are no auditable changes, EF Core saves once without a second save.
    /// </remarks>
    public override int SaveChanges()
    {
        ApplyCreatedAt();
        var pendingAudit = CreatePendingAuditEntries();
        if (pendingAudit.Count == 0)
            return base.SaveChanges();

        // Save entity changes and their audit rows atomically; generated entity IDs are known after the first save.
        using var transaction = Database.IsRelational() && Database.CurrentTransaction == null
            ? Database.BeginTransaction()
            : null;

        var result = base.SaveChanges();
        AddAuditEntries(pendingAudit);
        result += base.SaveChanges();
        transaction?.Commit();
        return result;
    }

    /// <summary>
    /// Asynchronously applies creation timestamps and saves tracked entity changes, recording audit entries when needed.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the save operation.</param>
    /// <returns>The total number of state entries written, including audit entries.</returns>
    /// <remarks>
    /// Entity changes are saved first so generated keys are available to the audit rows. For relational providers,
    /// both saves use the current transaction or a new transaction, preventing a failed audit write from leaving
    /// unaudited entity changes. If there are no auditable changes, EF Core saves once without a second save.
    /// </remarks>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyCreatedAt();
        var pendingAudit = CreatePendingAuditEntries();
        if (pendingAudit.Count == 0)
            return await base.SaveChangesAsync(cancellationToken);

        // Save entity changes and their audit rows atomically; generated entity IDs are known after the first save.
        await using var transaction = Database.IsRelational() && Database.CurrentTransaction == null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        var result = await base.SaveChangesAsync(cancellationToken);
        AddAuditEntries(pendingAudit);
        result += await base.SaveChangesAsync(cancellationToken);
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Converts the current tracked entity changes into audit data before the changes are persisted.
    /// </summary>
    /// <returns>Audit entries containing the action and changed values for supported entity types.</returns>
    /// <remarks>
    /// Many-to-many tag join rows are represented as Dream tag changes, and soft-delete state transitions are
    /// represented as delete or restore actions rather than ordinary updates. Unchanged updates are omitted, and
    /// multiple changes for the same entity and action are combined into one audit entry.
    /// </remarks>
    private List<PendingAuditEntry> CreatePendingAuditEntries()
    {
        ChangeTracker.DetectChanges();
        var changes = new List<PendingAuditChange>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            // EF tracks many-to-many tag links as shared join rows, so translate them into dream-level changes.
            if (entry.Metadata.Name == "DreamTag")
            {
                var dreamId = (int)entry.Property("DreamID").CurrentValue!;
                var tagId = (int)entry.Property("TagsID").CurrentValue!;
                var tagName = ChangeTracker.Entries<Tag>()
                    .FirstOrDefault(tag => tag.Entity.ID == tagId)?.Entity.Title ?? $"Tag {tagId}";
                var isAdded = entry.State == EntityState.Added;
                changes.Add(new PendingAuditChange(
                    "Dream",
                    entry.Entity,
                    dreamId,
                    ChangeTracker.Entries<Dream>()
                        .FirstOrDefault(dream => dream.Entity.ID == dreamId)?.Entity.CloudID ?? 0,
                    ChangeTracker.Entries<Dream>()
                        .FirstOrDefault(dream => dream.Entity.ID == dreamId)?.Entity.Name ?? "Dream",
                    "Updated",
                    new Dictionary<string, AuditChange>
                    {
                        ["Tags"] = new AuditChange(isAdded ? null : tagName, isAdded ? tagName : null)
                    }));
                continue;
            }

            var entity = entry.Entity;
            var (entityType, entityId, cloudId, entityName, prefix) = GetAuditEntityInfo(entity);

            if (entityType == null)
                continue;

            var commentDeleted = entity is Comment && entry.State == EntityState.Modified &&
                                 entry.Properties.Any(p => p.Metadata.Name == nameof(Models.Comment.DeletedUtc) &&
                                     p.IsModified && p.OriginalValue is null && p.CurrentValue is DateTime);

            // Soft-deleted entities remain Modified in EF, but should read as deletions in the log.
            var deleted = entry.State == EntityState.Deleted || commentDeleted ||
                          (entry.State == EntityState.Modified &&
                           entry.Properties.Any(p => p.Metadata.Name == "IsDeleted" &&
                               p.IsModified && p.CurrentValue is true));
            // A soft-delete reversal is a distinct lifecycle event, not an ordinary edit.
            var restored = entry.State == EntityState.Modified &&
                           entry.Properties.Any(p => p.Metadata.Name == "IsDeleted" &&
                               p.IsModified && p.OriginalValue is true && p.CurrentValue is false);
            var action = entry.State == EntityState.Added
                ? "Created"
                : deleted ? "Deleted" : restored ? "Restored" : "Updated";

            var values = new Dictionary<string, AuditChange>();
            foreach (var property in entry.Properties)
            {
                // Keep removed comment text out of the deletion event.
                if (property.Metadata.IsPrimaryKey() ||
                    (commentDeleted && property.Metadata.Name == nameof(Models.Comment.Body)))
                    continue;

                if (entry.State == EntityState.Added)
                {
                    values[PropertyName(prefix, property.Metadata.Name)] = new AuditChange(null, FormatValue(property.CurrentValue));
                }
                else if (entry.State == EntityState.Deleted || property.IsModified)
                {
                    var oldValue = property.OriginalValue;
                    var newValue = entry.State == EntityState.Deleted ? null : property.CurrentValue;
                    values[PropertyName(prefix, property.Metadata.Name)] =
                        new AuditChange(FormatValue(oldValue), FormatValue(newValue));
                }
            }

            if (values.Count == 0 && action == "Updated")
                continue;

            changes.Add(new PendingAuditChange(
                entityType,
                entity,
                entityId,
                cloudId,
                entityName ?? entityType,
                action,
                values));
        }

        return changes
            .GroupBy(x => new { x.EntityType, x.Entity, x.Action })
            .Select(group => new PendingAuditEntry(
                group.Key.EntityType,
                group.Key.Entity,
                group.First().EntityID,
                group.First().CloudID,
                group.First().EntityName,
                group.Key.Action,
                group.SelectMany(x => x.Changes).ToDictionary(x => x.Key, x => x.Value)))
            .ToList();
    }

    /// <summary>
    /// Adds the captured audit data to the context after entity changes have been saved.
    /// </summary>
    /// <param name="pendingAudit">The audit data captured before persistence.</param>
    /// <remarks>
    /// Entity metadata is resolved again after saving so generated IDs and current names are available; captured
    /// values are retained as fallbacks. The actor comes from the current HTTP user, with the identity ID used
    /// when no display name is available and System used when there is no user ID.
    /// </remarks>
    private void AddAuditEntries(IReadOnlyList<PendingAuditEntry> pendingAudit)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = user?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            userName = userId ?? "System";

        foreach (var pending in pendingAudit)
        {
            var (entityType, entityId, cloudId, entityName, _) = GetAuditEntityInfo(pending.Entity);

            AuditLog.Add(new AuditLogEntry
            {
                CreatedAtUtc = _appTime.UtcNow,
                CloudID = entityType == null ? pending.CloudID : cloudId,
                EntityType = entityType ?? pending.EntityType,
                EntityID = entityType == null ? pending.EntityID : entityId,
                EntityName = entityType == null ? pending.EntityName : entityName ?? pending.EntityName,
                Action = pending.Action,
                UserID = userId,
                UserName = userName,
                Changes = JsonSerializer.Serialize(pending.Changes)
            });
        }
    }

    /// <summary>
    /// Maps a supported entity to the identifying information needed for a cloud-scoped audit row.
    /// </summary>
    /// <param name="entity">The changed entity.</param>
    /// <returns>
    /// The audit type, entity ID, owning cloud ID, optional display name, and optional property-name prefix;
    /// an unsupported entity has a null type.
    /// </returns>
    private (string? EntityType, int EntityID, int CloudID, string? EntityName, string? Prefix)
        GetAuditEntityInfo(object entity)
    {
        return entity switch
        {
            Dream dream => ("Dream", dream.ID, dream.CloudID, dream.Name, null),
            Cloud cloud => ("Cloud", cloud.ID, cloud.ID, cloud.Name, null),
            FundingRound round => ("Funding round", round.ID, round.CloudID, null, null),
            CloudInfo info => ("Cloud", info.CloudID, info.CloudID, "Cloud info", "Info"),
            BudgetItem item => ("Budget item", item.ID, GetDreamCloudID(item.DreamID), item.Name, null),
            Dreamer dreamer => ("Dreamer", dreamer.ID, GetDreamCloudID(dreamer.DreamID),
                GetTrackedUserName(dreamer.UserID) ?? dreamer.UserID, null),
            Wallet wallet => ("Wallet", wallet.ID, wallet.CloudID,
                GetTrackedUserName(wallet.UserID) ?? wallet.UserID, null),
            Fund fund => ("Fund", fund.ID, GetFundCloudID(fund),
                $"Fund for {GetDreamName(fund.DreamID)}", null),
            Tag tag => ("Tag", tag.ID, tag.CloudID, tag.Title, null),
            Comment comment => ("Comment", comment.ID, GetDreamCloudID(comment.DreamID),
                $"Comment on {GetDreamName(comment.DreamID)}", null),
            Expense expense => ("Expense", expense.ID, GetDreamCloudID(expense.DreamID),
                expense.Description, null),
            _ => (null, 0, 0, null, null)
        };
    }

    /// <summary>
    /// Resolves a dream's cloud from tracked entities or the database, including soft-deleted dreams.
    /// </summary>
    /// <param name="dreamId">The dream whose owning cloud is needed.</param>
    /// <returns>The owning cloud ID, or zero if the dream cannot be found.</returns>
    /// <remarks>
    /// Looking in the change tracker first avoids an extra query; ignoring query filters lets deletion audit
    /// entries retain their cloud association after a dream has been soft-deleted.
    /// </remarks>
    private int GetDreamCloudID(int dreamId)
    {
        var trackedDream = ChangeTracker.Entries<Dream>()
            .FirstOrDefault(dream => dream.Entity.ID == dreamId)?.Entity;
        if (trackedDream != null)
            return trackedDream.CloudID;

        return Dream.IgnoreQueryFilters()
            .Where(dream => dream.ID == dreamId)
            .Select(dream => dream.CloudID)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets a dream's tracked name for use in a related entity's audit label.
    /// </summary>
    /// <param name="dreamId">The dream whose name is needed.</param>
    /// <returns>The tracked dream name, or a readable ID-based label when the dream is not tracked.</returns>
    private string GetDreamName(int dreamId) =>
        ChangeTracker.Entries<Dream>()
            .FirstOrDefault(dream => dream.Entity.ID == dreamId)?.Entity.Name ?? $"Dream {dreamId}";

    /// <summary>
    /// Resolves a fund's cloud through its tracked dream, funding round, or wallet relationship.
    /// </summary>
    /// <param name="fund">The fund being audited.</param>
    /// <returns>The related cloud ID, or zero if it cannot be resolved.</returns>
    private int GetFundCloudID(Fund fund)
    {
        var dreamCloudId = GetDreamCloudID(fund.DreamID);
        if (dreamCloudId != 0)
            return dreamCloudId;

        var fundingRound = fund.FundingRoundID.HasValue
            ? ChangeTracker.Entries<FundingRound>()
                .FirstOrDefault(round => round.Entity.ID == fund.FundingRoundID.Value)?.Entity
            : null;
        if (fundingRound != null)
            return fundingRound.CloudID;

        return ChangeTracker.Entries<Wallet>()
            .FirstOrDefault(wallet => wallet.Entity.UserID == fund.UserID)?.Entity.CloudID ?? 0;
    }

    /// <summary>
    /// Looks up a user's display name without issuing a database query.
    /// </summary>
    /// <param name="userId">The user's identity ID.</param>
    /// <returns>The tracked user's name, or <see langword="null"/> when the user is not tracked.</returns>
    private string? GetTrackedUserName(string userId) =>
        ChangeTracker.Entries<User>().FirstOrDefault(user => user.Entity.Id == userId)?.Entity.Name;

    /// <summary>
    /// Adds a parent-property prefix for changes originating from a nested entity.
    /// </summary>
    /// <param name="prefix">The optional parent-property name.</param>
    /// <param name="propertyName">The changed property name.</param>
    private static string PropertyName(string? prefix, string propertyName) =>
        prefix == null ? propertyName : $"{prefix}.{propertyName}";

    /// <summary>
    /// Formats a property value consistently for storage in the audit log.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> for a null value, an ISO 8601 string for date/time values, or an invariant string
    /// for other values.
    /// </returns>
    private static string? FormatValue(object? value) =>
        value switch
        {
            null => null,
            DateTime dateTime => dateTime.ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };

    private sealed record AuditChange(string? OldValue, string? NewValue);
    private sealed record PendingAuditChange(
        string EntityType,
        object Entity,
        int EntityID,
        int CloudID,
        string EntityName,
        string Action,
        Dictionary<string, AuditChange> Changes);
    private sealed record PendingAuditEntry(
        string EntityType,
        object Entity,
        int EntityID,
        int CloudID,
        string EntityName,
        string Action,
        Dictionary<string, AuditChange> Changes);

    /// <summary>
    /// Sets <c>CreatedAtUtc</c> on added entities that implement <see cref="IAutoPopulateCreatedAtOnAdd"/>
    /// and prevents the timestamp from being changed on existing entities.
    /// </summary>
    private void ApplyCreatedAt()
    {
        var entries = ChangeTracker
            .Entries<IAutoPopulateCreatedAtOnAdd>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.CreatedAtUtc = _appTime.UtcNow;
        }

        // Prevent modification of CreatedAt on existing entities
        var modifiedEntries = ChangeTracker
            .Entries<IAutoPopulateCreatedAtOnAdd>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            var property = entry.Property(e => e.CreatedAtUtc);
            if (property.IsModified)
            {
                property.CurrentValue = property.OriginalValue;
                property.IsModified = false;
            }
        }
    }
}