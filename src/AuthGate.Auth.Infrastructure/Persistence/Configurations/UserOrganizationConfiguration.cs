using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class UserOrganizationConfiguration : IEntityTypeConfiguration<UserOrganization>
{
    public void Configure(EntityTypeBuilder<UserOrganization> builder)
    {
        builder.ToTable("UserOrganizations");

        builder.HasKey(x => new { x.UserId, x.OrganizationId });

        builder.Property(x => x.RoleInOrg)
            .HasMaxLength(64);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.OrganizationId);

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserOrganizations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
