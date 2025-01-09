using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using AdminSERMAC.Services.Database.Interfaces;

namespace AdminSERMAC.Services.Database
{
    public class DatabaseService : BaseSQLiteService, IDatabaseService
    {
        public DatabaseService(ILogger<DatabaseService> logger, string connectionString)
            : base(logger, connectionString)
        {
        }

        public async Task<bool> EnsureTableExistsAsync(string tableName, string createTableSql)
        {
            try
            {
                await ExecuteInTransactionAsync(async (connection, transaction) =>
                {
                    using var command = new SQLiteCommand(createTableSql, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating table {tableName}");
                return false;
            }
        }

        public async Task<bool> EnsureColumnExistsAsync(string tableName, string columnName, string columnType)
        {
            try
            {
                await ExecuteInTransactionAsync(async (connection, transaction) =>
                {
                    if (!ColumnExists(connection, tableName, columnName))
                    {
                        string alterTableSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
                        using var command = new SQLiteCommand(alterTableSql, connection, transaction);
                        await command.ExecuteNonQueryAsync();
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ensuring column {columnName} exists in table {tableName}");
                return false;
            }
        }

        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                using var sourceConnection = new SQLiteConnection(_connectionString);
                using var destinationConnection = new SQLiteConnection($"Data Source={backupPath}");
                await sourceConnection.OpenAsync();
                await destinationConnection.OpenAsync();

                sourceConnection.BackupDatabase(destinationConnection, "main", "main", -1, null, 0);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
                return false;
            }
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating database connection");
                return false;
            }
        }

        public string GetConnectionString() => _connectionString;
    }
}
