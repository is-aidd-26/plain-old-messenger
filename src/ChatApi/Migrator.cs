using System.Reflection;

using Microsoft.Extensions.Logging;

using Npgsql;

namespace ChatApi;

/// <summary>
///  Применяет SQL-миграции при старте приложения. Файлы лежат в каталоге Migrations/,
///  встраиваются в сборку как ресурсы и выполняются по порядку имён. Применённые
///  версии фиксируются в служебной таблице schema_migrations, поэтому повторный
///  старт ничего не меняет.
/// </summary>
public static class Migrator
{
    private const string Prefix = "ChatApi.Migrations.";
    private static readonly TimeSpan WaitForDatabase = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryPause = TimeSpan.FromSeconds(1);

    /// <summary>
    ///  Ждёт готовности базы и применяет все неприменённые миграции.
    /// </summary>
    public static async Task MigrateAsync(NpgsqlDataSource dataSource, ILogger logger)
    {
        NpgsqlConnection connection = await OpenWithRetryAsync(dataSource, logger);
        await using (connection)
        {
            await ApplyPendingAsync(connection, logger);
        }
    }

    private static async Task<NpgsqlConnection> OpenWithRetryAsync(NpgsqlDataSource dataSource, ILogger logger)
    {
        // База в docker compose может подниматься дольше, чем приложение, — пробуем подключиться
        // несколько раз, прежде чем сдаться.
        DateTime deadline = DateTime.UtcNow + WaitForDatabase;
        while (true)
        {
            try
            {
                return await dataSource.OpenConnectionAsync();
            }
            catch (NpgsqlException e) when (DateTime.UtcNow < deadline)
            {
                logger.LogInformation(e, "База ещё не готова, повторяем попытку…");
                await Task.Delay(RetryPause);
            }
        }
    }

    private static async Task ApplyPendingAsync(NpgsqlConnection connection, ILogger logger)
    {
        await using NpgsqlCommand command = CreateCommand(
            connection,
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version     text        NOT NULL PRIMARY KEY,
                applied_at  timestamptz NOT NULL DEFAULT now()
            )
            """);
        await command.ExecuteNonQueryAsync();

        Assembly assembly = typeof(Migrator).Assembly;
        foreach (string resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
                     .Order(StringComparer.Ordinal))
        {
            string version = resourceName[Prefix.Length..^".sql".Length];
            if (await IsAppliedAsync(connection, version))
            {
                continue;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Ресурс миграции не найден: {resourceName}");
            using StreamReader reader = new(stream);
            string script = await reader.ReadToEndAsync();

            // Миграция применяется в транзакции вместе с записью о версии: либо всё, либо ничего.
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
            await ExecuteAsync(connection, transaction, script);
            await ExecuteAsync(connection, transaction, "INSERT INTO schema_migrations (version) VALUES ($1)", version);
            await transaction.CommitAsync();

            logger.LogInformation("Применена миграция {Version}", version);
        }
    }

    private static async Task<bool> IsAppliedAsync(NpgsqlConnection connection, string version)
    {
        await using NpgsqlCommand command = CreateCommand(
            connection,
            "SELECT 1 FROM schema_migrations WHERE version = $1",
            version);
        return await command.ExecuteScalarAsync() is not null;
    }

    private static Task<int> ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, params string[] values)
    {
        NpgsqlCommand command = CreateCommand(connection, sql, values);
        command.Transaction = transaction;
        return command.ExecuteNonQueryAsync();
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, params string[] values)
    {
        NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (string value in values)
        {
            command.Parameters.AddWithValue(value);
        }

        return command;
    }
}