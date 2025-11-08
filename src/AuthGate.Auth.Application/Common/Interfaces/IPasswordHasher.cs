namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service interface for password hashing and verification
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    /// <param name="hash">Password hash</param>
    /// <param name="password">Plain text password to verify</param>
    /// <returns>True if password matches hash, false otherwise</returns>
    bool VerifyPassword(string hash, string password);
}
