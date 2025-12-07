using AuthGate.Auth.Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Service for generating device fingerprints based on User-Agent and IP address
/// </summary>
public class DeviceFingerprintService : IDeviceFingerprintService
{
    public string GenerateFingerprint(string? userAgent, string? ipAddress)
    {
        // Combine User-Agent and IP for fingerprint
        var rawData = $"{userAgent ?? "unknown"}|{ipAddress ?? "unknown"}";
        
        // Generate SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToBase64String(hashBytes);
    }

    public string GetDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        var ua = userAgent.ToLower();

        // Detect browser
        string browser = ua switch
        {
            var s when s.Contains("edge") || s.Contains("edg/") => "Edge",
            var s when s.Contains("chrome") && !s.Contains("edge") => "Chrome",
            var s when s.Contains("firefox") => "Firefox",
            var s when s.Contains("safari") && !s.Contains("chrome") => "Safari",
            var s when s.Contains("opera") || s.Contains("opr/") => "Opera",
            _ => "Browser"
        };

        // Detect OS
        string os = ua switch
        {
            var s when s.Contains("windows") => "Windows",
            var s when s.Contains("mac") => "macOS",
            var s when s.Contains("linux") => "Linux",
            var s when s.Contains("android") => "Android",
            var s when s.Contains("iphone") || s.Contains("ipad") => "iOS",
            _ => "Unknown OS"
        };

        return $"{browser} on {os}";
    }
}
