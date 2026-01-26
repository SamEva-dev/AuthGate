using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class ManagerInvitationConfiguration : IEntityTypeConfiguration<ManagerInvitation>
{
    public void Configure(EntityTypeBuilder<ManagerInvitation> builder)
    {
        builder.ToTable("ManagerInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RoleIdsCsv)
            .HasColumnType("text");

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.TokenHash);
        builder.HasIndex(x => new { x.OrganizationId, x.Email });
    }
}
