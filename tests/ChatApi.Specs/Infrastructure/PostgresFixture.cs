using Testcontainers.PostgreSql;

namespace ChatApi.Specs.Infrastructure;

/// <summary>
///  Один контейнер PostgreSQL 18 на весь прогон тестов: изоляция достигается отдельной
///  базой данных на каждый сценарий, а не отдельным контейнером. Запускается хуком
///  BeforeTestRun, останавливается хуком AfterTestRun.
/// </summary>
public sealed class PostgresFixture
{
    private const string PostgresImage = "postgres:18";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgresImage).Build();

    /// <summary>
    ///  Строка подключения к служебной базе контейнера: через неё тесты создают
    ///  и удаляют собственные базы данных.
    /// </summary>
    public string AdminConnectionString => _container.GetConnectionString();

    /// <summary> Запускает контейнер один раз перед прогоном тестов. </summary>
    public Task StartAsync() => _container.StartAsync();

    /// <summary> Останавливает контейнер после прогона тестов. </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}