using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace EcoBilling.Infrastructure.Concurrency;

public sealed class PostgreSqlWorkerExecutionLock(NpgsqlDataSource dataSource)
    : IWorkerExecutionLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string lockName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockName);

        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            var lockId = CreateStableLockId(lockName);
            await using var command = new NpgsqlCommand(
                "SELECT pg_try_advisory_lock(@lock_id)",
                connection);
            command.Parameters.AddWithValue("lock_id", lockId);

            var acquired = (bool?)await command.ExecuteScalarAsync(cancellationToken)
                ?? false;

            if (!acquired)
            {
                await connection.DisposeAsync();
                return null;
            }

            return new PostgreSqlWorkerExecutionLease(connection, lockId);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static long CreateStableLockId(string lockName)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"ecobilling-worker:{lockName}"));
        return BitConverter.ToInt64(bytes, 0);
    }

    private sealed class PostgreSqlWorkerExecutionLease(
        NpgsqlConnection connection,
        long lockId)
        : IAsyncDisposable
    {
        private int disposed;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            try
            {
                await using var command = new NpgsqlCommand(
                    "SELECT pg_advisory_unlock(@lock_id)",
                    connection);
                command.Parameters.AddWithValue("lock_id", lockId);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
