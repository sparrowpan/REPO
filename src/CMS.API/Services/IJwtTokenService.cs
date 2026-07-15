using CMS.API.Models;

namespace CMS.API.Services;

/// <summary>Builds signed JWT access tokens for authenticated users.</summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Create a JWT signed (HMAC-SHA256) with <paramref name="signingSecret"/>, carrying the
    /// user's id, name, and one role claim per assigned role. The token expires 24 hours after issue.
    /// </summary>
    string CreateToken(LoginCredential credential, string signingSecret);
}
