using Dapper;
using Npgsql;

namespace NetSimpleAuctioneer.API.Database
{
    /// <summary>
    /// Represents a database connection abstraction for performing asynchronous database queries.
    /// This interface defines the contract for interacting with a database connection and executing queries.
    /// The usage of the interface allows unit testing, adding the possibility to mock
    /// </summary>
    public interface IDatabaseConnection : IDisposable
    {
        Task<IEnumerable<T>> QueryAsync<T>(CommandDefinition command);

        Task<T?> QuerySingleOrDefaultAsync<T>(CommandDefinition command);
    }

    public class NpgsqlDatabaseConnection(string connectionString) : IDatabaseConnection
    {
        private readonly NpgsqlConnection _connection = new(connectionString);

        public Task<IEnumerable<T>> QueryAsync<T>(CommandDefinition command)
        {
            return _connection.QueryAsync<T>(command);
        }

        public Task<T?> QuerySingleOrDefaultAsync<T>(CommandDefinition command)
        {
            return _connection.QuerySingleOrDefaultAsync<T>(command);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
