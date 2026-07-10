using Microsoft.EntityFrameworkCore;
using ResearchRag.Domain.Common;
using ResearchRag.Domain.Entities;

namespace ResearchRag.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OneTimeToken> OneTimeTokens => Set<OneTimeToken>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Citation> Citations => Set<Citation>();
    public DbSet<PaperExtraction> PaperExtractions => Set<PaperExtraction>();
    public DbSet<QueryLog> QueryLogs => Set<QueryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().Property(x => x.Email).HasMaxLength(320);
        modelBuilder.Entity<User>().Property(x => x.PasswordHash).HasMaxLength(512);
        modelBuilder.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(32);

        // Indexed string columns need bounded lengths: without HasMaxLength they
        // map to longtext, which MySQL cannot index (error 1170).
        modelBuilder.Entity<RefreshToken>().Property(x => x.TokenHash).HasMaxLength(128);
        modelBuilder.Entity<RefreshToken>().Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.FamilyId);
        modelBuilder.Entity<OneTimeToken>().Property(x => x.TokenHash).HasMaxLength(128);
        modelBuilder.Entity<OneTimeToken>().Property(x => x.Purpose).HasMaxLength(64);
        modelBuilder.Entity<OneTimeToken>().HasIndex(x => new { x.TokenHash, x.Purpose }).IsUnique();

        modelBuilder.Entity<Workspace>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Workspace>().HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        modelBuilder.Entity<Document>().Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        modelBuilder.Entity<Document>().HasIndex(x => new { x.WorkspaceId, x.Status });
        modelBuilder.Entity<DocumentChunk>().HasIndex(x => new { x.WorkspaceId, x.DocumentId });
        // No index on DocumentChunk.Text: it maps to longtext, which MySQL cannot
        // index without a prefix length (error 1170), and a btree index would not
        // serve the LIKE '%term%' keyword search anyway.
        modelBuilder.Entity<ProcessingJob>().Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        modelBuilder.Entity<ProcessingJob>().HasIndex(x => x.Status);
        modelBuilder.Entity<PaperExtraction>().HasIndex(x => x.DocumentId).IsUnique();

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entity.ClrType))
            {
                modelBuilder.Entity(entity.ClrType).Property(nameof(Entity.CreatedAt));
                modelBuilder.Entity(entity.ClrType).Property(nameof(Entity.UpdatedAt));
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

