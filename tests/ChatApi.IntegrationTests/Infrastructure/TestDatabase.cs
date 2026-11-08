using Npgsql;

namespace ChatApi.IntegrationTests.Infrastructure;

/// <summary>
///  Создаёт и удаляет базы данных внутри контейнера PostgreSQL. Имя базы генерируется
///  самими тестами, поэтому подстановка имени в SQL безопасна.
/// </summary>
public static class TestDatabase
{
    /// <summary> Создаёт пустую базу данных с указанным именем. </summary>
    public static async Task CreateAsync(string adminConnectionString, string name)
    {
        await ExecuteAsync(adminConnectionString, $"CREATE DATABASE \"{name}\"");
    }

    /// <summary> Удаляет базу данных вместе с оставшимися подключениями к ней. </summary>
    public static async Task DropAsync(string adminConnectionString, string name)
    {
        await ExecuteAsync(adminConnectionString, $"DROP DATABASE \"{name}\" WITH (FORCE)");
    }

    /// <summary> Строка подключения к указанной базе данных внутри контейнера. </summary>
    public static string ConnectionString(string adminConnectionString, string name)
    {
        return new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = name }.ConnectionString;
    }

    private static async Task ExecuteAsync(string adminConnectionString, string sql)
    {
        await using NpgsqlConnection connection = new(adminConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}