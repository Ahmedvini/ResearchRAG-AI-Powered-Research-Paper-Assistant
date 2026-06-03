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

        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<OneTimeToken>().HasIndex(x => new { x.TokenHash, x.Purpose }).IsUnique();

        modelBuilder.Entity<Workspace>().HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        modelBuilder.Entity<Document>().Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        modelBuilder.Entity<Document>().HasIndex(x => new { x.WorkspaceId, x.Status });
        modelBuilder.Entity<DocumentChunk>().HasIndex(x => new { x.WorkspaceId, x.DocumentId });
        modelBuilder.Entity<DocumentChunk>().HasIndex(x => x.Text).HasDatabaseName("IX_DocumentChunks_Text");
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

