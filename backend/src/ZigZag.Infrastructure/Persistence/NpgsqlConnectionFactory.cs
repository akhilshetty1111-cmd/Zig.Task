using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ZigZag.Application.Common.Interfaces;

namespace ZigZag.Infrastructure.Persistence;

/// <summary>
/// Opens PostgreSQL connections using the "ZigZagDb" connection string.
/// </summary>
/// <remarks>
/// Registered as scoped (see <c>InfrastructureServiceCollectionExtensions</c>),
/// so one instance - and, within a unit of work, one connection - is reused
/// per HTTP request rather than a fresh physical connection being opened for
/// every repository call. Npgsql pools physical connections internally, so
/// this does not defeat pooling.
/// </remarks>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ZigZagDb")
            ?? throw new InvalidOperationException(
                "Connection string 'ZigZagDb' is not configured. Set ConnectionStrings:ZigZagDb via " +
                "appsettings, user secrets, or the ConnectionStrings__ZigZagDb environment variable.");
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
