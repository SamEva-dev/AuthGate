using System.Text.Json;
using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
namespace AuthGate.Auth.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly string _connectionString;

    public AuditRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Audit")
            ?? "Data Source=Data/AuthAudit.db";
    }

    public async Task<IEnumerable<AuditLogDto>> GetRecentAsync(int limit = 50)
    {
        using var conn = new SqliteConnection($"Data Source= {_connectionString}");
        var rows = await conn.QueryAsync("SELECT Timestamp, Level, Message, Properties FROM Logs ORDER BY Timestamp DESC LIMIT @Limit", new { Limit = limit });

        return rows.Select(r =>
        {
            var props = JsonSerializer.Deserialize<Dictionary<string, object>>(r.Properties ?? "{}");
            return new AuditLogDto
            {
                Timestamp = DateTime.Parse(r.Timestamp),
                Level = r.Level,
                Message = r.Message,
                AuditType = props?.GetValueOrDefault("AuditType")?.ToString(),
                UserId = props?.GetValueOrDefault("UserId")?.ToString(),
                Email = props?.GetValueOrDefault("Email")?.ToString(),
                IpAddress = props?.GetValueOrDefault("IpAddress")?.ToString(),
                UserAgent = props?.GetValueOrDefault("UserAgent")?.ToString()
            };
        });
    }

    public async Task AddAsync(AuditLogDto entry)
    {
        //using var conn = new SqliteConnection($"Data Source= {_connectionString}");
        //await conn.ExecuteAsync(@"
        //    INSERT INTO Logs (Timestamp, Level, Message, Properties)
        //    VALUES (@Timestamp, @Level, @Message, @Properties)",
        //    new
        //    {
        //        entry.Timestamp,
        //        entry.Level,
        //        entry.Message,
        //        Properties = JsonSerializer.Serialize(new
        //        {
        //            entry.AuditType,
        //            entry.UserId,
        //            entry.Email,
        //            entry.IpAddress,
        //            entry.UserAgent
        //        })
        //    });
    }
}
