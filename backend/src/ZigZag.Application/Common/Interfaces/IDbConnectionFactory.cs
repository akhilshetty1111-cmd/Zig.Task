using System.Data;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Creates open ADO.NET connections to the ZigZag database.
/// </summary>
/// <remarks>
/// Declared in Application (not Infrastructure) because handlers depend on
/// this abstraction directly - Application must not reference Npgsql to stay
/// persistence-agnostic. Infrastructure provides the Npgsql implementation.
/// Callers own the returned connection and must dispose it; the factory does
/// not pool connections itself beyond what Npgsql already does internally.
/// </remarks>
public interface IDbConnectionFactory
{
    /// <summary>Opens a new database connection. The caller disposes it.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
