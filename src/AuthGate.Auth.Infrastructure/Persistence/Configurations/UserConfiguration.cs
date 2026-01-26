using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.OrganizationId)
            .HasColumnName("organization_id");

        builder.Property(u => u.MustChangePassword)
            .HasDefaultValue(false)
            .HasColumnName("must_change_password");

        builder.Property(u => u.MustChangePasswordBeforeUtc)
            .HasColumnName("must_change_password_before_utc");

        builder.Property(u => u.PasswordLastChangedAtUtc)
            .HasColumnName("password_last_changed_at_utc");

        builder.HasIndex(u => u.OrganizationId);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.MfaSecret)
            .WithOne(ms => ms.User)
            .HasForeignKey<MfaSecret>(ms => ms.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RecoveryCodes)
            .WithOne(rc => rc.User)
            .HasForeignKey(rc => rc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PasswordResetTokens)
            .WithOne(prt => prt.User)
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
