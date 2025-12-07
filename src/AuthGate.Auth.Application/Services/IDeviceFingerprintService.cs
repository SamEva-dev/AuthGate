namespace AuthGate.Auth.Application.Services;

/// <summary>
/// Service interface for generating and managing device fingerprints
/// </summary>
public interface IDeviceFingerprintService
{
    /// <summary>
    /// Generates a device fingerprint from User-Agent and IP address
    /// </summary>
    /// <param name="userAgent">The User-Agent string from the request</param>
    /// <param name="ipAddress">The IP address of the request</param>
    /// <returns>A unique fingerprint string</returns>
    string GenerateFingerprint(string? userAgent, string? ipAddress);

    /// <summary>
    /// Extracts a friendly device name from User-Agent
    /// </summary>
    /// <param name="userAgent">The User-Agent string</param>
    /// <returns>A friendly device name (e.g., "Chrome on Windows")</returns>
    string GetDeviceName(string? userAgent);
}
