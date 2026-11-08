using Testcontainers.PostgreSql;
using Xunit;

namespace ChatApi.IntegrationTests.Infrastructure;

/// <summary>
///  Один контейнер PostgreSQL 18 на весь прогон тестов: изоляция достигается отдельной
///  базой данных на каждый тест, а не отдельным контейнером.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:18";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgresImage).Build();

    /// <summary>
    ///  Строка подключения к служебной базе контейнера: через неё тесты создают
    ///  и удаляют собственные базы данных.
    /// </summary>
    public string AdminConnectionString => _container.GetConnectionString();

    /// <summary> Запускает контейнер один раз перед тестами коллекции. </summary>
    public Task InitializeAsync() => _container.StartAsync();

    /// <summary> Останавливает контейнер после тестов коллекции. </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}