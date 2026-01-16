using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthGate.Auth.Infrastructure.Persistence.Configurations;

public class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("UserInvitations");
        
        builder.HasKey(ui => ui.Id);
        
        builder.Property(ui => ui.OrganizationId)
            .IsRequired();
        
        builder.Property(ui => ui.OrganizationCode)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(ui => ui.OrganizationName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(ui => ui.Email)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(ui => ui.Role)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(ui => ui.TokenHash)
            .IsRequired()
            .HasMaxLength(128); // SHA256 hex = 64 chars, with margin
        
        builder.Property(ui => ui.Status)
            .IsRequired();
        
        builder.Property(ui => ui.Message)
            .HasMaxLength(1000);
        
        // Indexes
        builder.HasIndex(ui => ui.TokenHash)
            .IsUnique();
        
        builder.HasIndex(ui => new { ui.OrganizationId, ui.Email });
        
        builder.HasIndex(ui => new { ui.Email, ui.Status });
        
        builder.HasIndex(ui => ui.ExpiresAt);
    }
}
