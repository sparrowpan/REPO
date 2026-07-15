using System.Text;
using CMS.API.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace CMS.API.Services;

/// <summary>
/// Supplies the JWT validation signing key — the same <c>symmetricSecurityKey</c> from
/// <c>SysConfig</c> that <see cref="JwtTokenService"/> signs tokens with. The key is read once
/// (via <see cref="IAuthRepository.GetSigningSecretAsync"/>) and cached for the process lifetime,
/// so token validation and issuance always agree without a per-request database round-trip.
/// </summary>
public sealed class SigningKeyProvider(IServiceProvider services)
{
    private readonly object _gate = new();
    private SecurityKey[] _keys = [];
    private bool _loaded;

    /// <summary>
    /// The signing keys for <c>IssuerSigningKeyResolver</c>. Empty when no secret is configured,
    /// which makes every token fail validation (401) rather than validate against a phantom key.
    /// </summary>
    public IEnumerable<SecurityKey> ResolveKeys()
    {
        if (_loaded)
            return _keys;

        lock (_gate)
        {
            if (_loaded)
                return _keys;

            using var scope = services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var secret = repository.GetSigningSecretAsync().GetAwaiter().GetResult();

            _keys = string.IsNullOrEmpty(secret)
                ? []
                : [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))];
            _loaded = true;
            return _keys;
        }
    }
}
