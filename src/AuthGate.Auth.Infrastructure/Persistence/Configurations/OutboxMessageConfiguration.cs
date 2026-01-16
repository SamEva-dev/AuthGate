using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(o => o.LastError)
            .HasMaxLength(2000);

        builder.Property(o => o.CorrelationId)
            .HasMaxLength(100);

        builder.HasIndex(o => o.ProcessedAtUtc)
            .HasFilter("\"ProcessedAtUtc\" IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Pending");

        builder.HasIndex(o => o.NextRetryAtUtc)
            .HasDatabaseName("IX_OutboxMessages_NextRetry");

        builder.HasIndex(o => o.RelatedEntityId)
            .HasDatabaseName("IX_OutboxMessages_RelatedEntity");

        builder.HasIndex(o => new { o.IsFailed, o.ProcessedAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Status");
    }
}
