using AuthGate.Auth.Domain.Common;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence;

/// <summary>
/// Separate database context for audit logs
/// </summary>
public class AuditDbContext : DbContext, IAuditDbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Ignore pending model changes warning (we manage migrations carefully)
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(al => al.Id);

            entity.Property(al => al.Action)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(al => al.Description)
                .HasMaxLength(1000);

            entity.Property(al => al.IpAddress)
                .HasMaxLength(45);

            entity.Property(al => al.UserAgent)
                .HasMaxLength(500);

            entity.Property(al => al.Metadata)
                .HasColumnType("jsonb");

            entity.Property(al => al.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasIndex(al => al.UserId);
            entity.HasIndex(al => al.Action);
            entity.HasIndex(al => al.CreatedAtUtc);

            // Note: No FK to ApplicationUser since it's in a separate database
            entity.Ignore(al => al.User);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set timestamps for entities
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
