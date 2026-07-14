using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based slim lookup queries used to populate FK / n-n select options.</summary>
public sealed class LookupRepository(IDbConnectionFactory connectionFactory) : ILookupRepository
{
    public async Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppUserLookup>(new CommandDefinition("""
            SELECT UserId, UserName
            FROM AppUser
            ORDER BY UserName
            """, cancellationToken: ct));
        return rows.ToList();
    }
}
