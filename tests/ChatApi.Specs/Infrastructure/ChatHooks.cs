using ChatApi.Specs.Client;

using Reqnroll;

namespace ChatApi.Specs.Infrastructure;

/// <summary>
///  Хуки жизненного цикла Reqnroll: контейнер PostgreSQL живёт весь прогон,
///  а каждый сценарий получает собственную базу данных и экземпляр приложения.
/// </summary>
[Binding]
public sealed class ChatHooks
{
    private static PostgresFixture? s_postgres;

    private readonly ChatScenarioState _state;
    private ChatApiFactory? _api;
    private string _databaseName = string.Empty;

    public ChatHooks(ChatScenarioState state)
    {
        this._state = state;
    }

    /// <summary> Запускает общий контейнер PostgreSQL один раз на прогон тестов. </summary>
    [BeforeTestRun]
    public static async Task StartPostgresAsync()
    {
        s_postgres = new PostgresFixture();
        await s_postgres.StartAsync();
    }

    /// <summary> Создаёт базу сценария и поднимает приложение, подключённое к ней. </summary>
    [BeforeScenario]
    public async Task PrepareScenarioAsync()
    {
        _state.StartedAt = DateTimeOffset.UtcNow;
        _databaseName = $"chat_spec_{Guid.NewGuid():N}";
        await TestDatabase.CreateAsync(s_postgres!.AdminConnectionString, _databaseName);
        _api = new ChatApiFactory(TestDatabase.ConnectionString(s_postgres.AdminConnectionString, _databaseName));
        _state.Client = new ChatClient(_api.CreateClient());
    }

    /// <summary> Останавливает приложение и удаляет базу сценария. </summary>
    [AfterScenario]
    public async Task TeardownScenarioAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
            await TestDatabase.DropAsync(s_postgres!.AdminConnectionString, _databaseName);
        }
    }

    /// <summary> Останавливает общий контейнер PostgreSQL после прогона тестов. </summary>
    [AfterTestRun]
    public static async Task StopPostgresAsync()
    {
        if (s_postgres is not null)
        {
            await s_postgres.DisposeAsync();
        }
    }
}