

using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthGate.Auth.Infrastructure.Persistence;

public class AuthGateDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DeviceSession> DeviceSessions => Set<DeviceSession>();
    public DbSet<UserLoginAttempt> UserLoginAttempts => Set<UserLoginAttempt>();

    public AuthGateDbContext(DbContextOptions<AuthGateDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(255).IsRequired();
        });

        b.Entity<DeviceSession>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.RefreshToken }).IsUnique();
            e.Property(x => x.RefreshToken).HasMaxLength(256).IsRequired();
            e.Property(x => x.UserAgent).HasMaxLength(512).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
        });

        b.Entity<UserLoginAttempt>(e =>
        {
            e.HasIndex(x => x.Email);
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
        });
    }
}
