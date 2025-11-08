using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(al => al.Description)
            .HasMaxLength(1000);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.Metadata)
            .HasColumnType("jsonb");

        builder.Property(al => al.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => al.Action);
        builder.HasIndex(al => al.CreatedAtUtc);

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
