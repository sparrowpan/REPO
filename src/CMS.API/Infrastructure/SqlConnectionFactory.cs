using System.Data;
using Microsoft.Data.SqlClient;

namespace CMS.API.Infrastructure;

/// <summary>SQL Server connection factory backed by the "CMS" connection string.</summary>
public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("CMS")
        ?? throw new InvalidOperationException("Missing connection string 'CMS'.");

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
