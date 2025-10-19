

using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthGate.Auth.Infrastructure.Persistence;

public class AuthGateDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DeviceSession> DeviceSessions => Set<DeviceSession>();
    public DbSet<UserLoginAttempt> UserLoginAttempts => Set<UserLoginAttempt>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public AuthGateDbContext(DbContextOptions<AuthGateDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(255).IsRequired();
        });

        builder.Entity<DeviceSession>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.RefreshToken }).IsUnique();
            e.Property(x => x.RefreshToken).HasMaxLength(256).IsRequired();
            e.Property(x => x.UserAgent).HasMaxLength(512).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
        });

        builder.Entity<UserLoginAttempt>(e =>
        {
            e.HasIndex(x => x.Email);
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
        });

        builder.Entity<UserRole>()
       .HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        builder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        builder.Entity<RolePermission>()
        .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        builder.Entity<PasswordResetToken>().HasIndex(x => x.Token).IsUnique();
    }
}
