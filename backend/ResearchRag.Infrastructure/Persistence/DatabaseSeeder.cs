using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ResearchRag.Domain.Entities;
using ResearchRag.Domain.Enums;

namespace ResearchRag.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (db.Database.IsInMemory())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            // Relational databases use migrations so the schema can evolve
            // without dropping data.
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hasher = new PasswordHasher<User>();

        // Production bootstrap: a custom admin from configuration, so public
        // deployments never ship the well-known demo credentials.
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var bootstrapAdmin = new User
            {
                Email = adminEmail.Trim().ToLowerInvariant(),
                DisplayName = "Administrator",
                PasswordHash = "",
                EmailVerified = true,
                Role = UserRole.Admin
            };
            bootstrapAdmin.PasswordHash = hasher.HashPassword(bootstrapAdmin, adminPassword);
            db.Users.Add(bootstrapAdmin);
        }

        // Demo users with well-known passwords: enabled by default for local
        // development; disable with Seed:DemoUsers=false on public deployments.
        var seedDemoUsers = !string.Equals(configuration["Seed:DemoUsers"], "false", StringComparison.OrdinalIgnoreCase);
        if (seedDemoUsers)
        {
            var admin = new User
            {
                Email = "admin@researchrag.local",
                DisplayName = "ResearchRAG Admin",
                PasswordHash = "",
                EmailVerified = true,
                Role = UserRole.Admin
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

            var user = new User
            {
                Email = "user@researchrag.local",
                DisplayName = "Demo Researcher",
                PasswordHash = "",
                EmailVerified = true,
                Role = UserRole.User
            };
            user.PasswordHash = hasher.HashPassword(user, "User123!");

            var workspace = new Workspace
            {
                User = user,
                Name = "Machine Learning",
                Description = "Seed workspace for sample papers and chats."
            };

            db.Users.AddRange(admin, user);
            db.Workspaces.Add(workspace);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
