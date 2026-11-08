using Xunit;

namespace ChatApi.IntegrationTests.Infrastructure;

/// <summary> Коллекция тестов, разделяющая один контейнер PostgreSQL. </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    /// <summary> Имя коллекции для атрибута [Collection] тестов. </summary>
    public const string Name = "postgres";
}