using Microsoft.Data.Sqlite;
using System.Text.Json;
using MyWebService.Models;

namespace MyWebService.Services
{
    public class ErrorLogger
    {
        private readonly string _connectionString;

        public ErrorLogger(string databasePath = "errors.db")
        {
            _connectionString = $"Data Source={databasePath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var createTableCommand = @"
                CREATE TABLE IF NOT EXISTS error_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    error_level TEXT,
                    error_message TEXT,
                    stack_trace TEXT,
                    user_id INTEGER,
                    chat_id INTEGER,
                    command TEXT,
                    additional_data TEXT
                )";

                using var command = new SqliteCommand(createTableCommand, connection);
                command.ExecuteNonQuery();
                Console.WriteLine("✅ SQLite таблица error_logs готова");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при создании таблицы: {ex.Message}");
            }
        }

        public async Task LogErrorAsync(Exception error,
                              string errorLevel = "ERROR",
                              long? userId = null,
                              long? chatId = null,
                              string? command = null,
                              object? additionalData = null)
        {
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var additionalDataJson = additionalData != null
                    ? JsonSerializer.Serialize(additionalData, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
                    : null;

                var sqlQuery = @"
                INSERT INTO error_logs 
                (error_level, error_message, stack_trace, user_id, chat_id, command, additional_data)
                VALUES (@errorLevel, @errorMessage, @stackTrace, @userId, @chatId, @command, @additionalData)";

                await using var dbCommand = new SqliteCommand(sqlQuery, connection);
                dbCommand.Parameters.AddWithValue("@errorLevel", errorLevel);
                dbCommand.Parameters.AddWithValue("@errorMessage", error.Message);
                dbCommand.Parameters.AddWithValue("@stackTrace", error.StackTrace ?? "");
                dbCommand.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
                dbCommand.Parameters.AddWithValue("@chatId", chatId ?? (object)DBNull.Value);
                dbCommand.Parameters.AddWithValue("@command", command ?? (object)DBNull.Value);
                dbCommand.Parameters.AddWithValue("@additionalData", additionalDataJson ?? (object)DBNull.Value);

                await dbCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"📝 Ошибка записана в БД: {error.Message}");
            }
            catch (Exception dbError)
            {
                Console.WriteLine($"❌ Не удалось записать ошибку в БД: {dbError.Message}");
                Console.WriteLine($"📝 Исходная ошибка: {error.Message}");
            }
        }

        public async Task<List<ErrorLog>> GetRecentErrorsAsync(int limit = 10)
        {
            var errors = new List<ErrorLog>();
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var selectCommand = @"
                SELECT timestamp, error_level, error_message, user_id, command, additional_data
                FROM error_logs 
                ORDER BY timestamp DESC 
                LIMIT @limit";

                await using var command = new SqliteCommand(selectCommand, connection);
                command.Parameters.AddWithValue("@limit", limit);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    errors.Add(new ErrorLog
                    {
                        Timestamp = reader.GetDateTime(0),
                        ErrorLevel = reader.GetString(1),
                        ErrorMessage = reader.GetString(2),
                        UserId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                        Command = reader.IsDBNull(4) ? null : reader.GetString(4),
                        AdditionalData = reader.IsDBNull(5) ? null : reader.GetString(5)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении ошибок: {ex.Message}");
            }
            return errors;
        }
    }
}