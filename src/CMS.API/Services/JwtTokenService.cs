using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CMS.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Services;

/// <summary>Issues 24-hour HMAC-SHA256 JWTs. Claim types: <c>userId</c>, <c>userName</c>, and role.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    /// <summary>Token lifetime — the token expires 24 hours after issue.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public string CreateToken(LoginCredential credential, string signingSecret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("userId", credential.UserId),
            new("userName", credential.UserName),
        };
        claims.AddRange(credential.RoleIds.Select(roleId => new Claim(ClaimTypes.Role, roleId)));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifetime),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
