using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class UserAppAccessConfiguration : IEntityTypeConfiguration<UserAppAccess>
{
    public void Configure(EntityTypeBuilder<UserAppAccess> builder)
    {
        builder.ToTable("UserAppAccess");

        builder.HasKey(x => new { x.UserId, x.AppId });

        builder.Property(x => x.AppId)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.GrantedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AppId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
