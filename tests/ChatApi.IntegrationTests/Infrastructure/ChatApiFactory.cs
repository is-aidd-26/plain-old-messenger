using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChatApi.IntegrationTests.Infrastructure;

/// <summary>
///  Поднимает ChatApi в памяти на TestServer и подключает его к указанной базе данных.
///  Миграции применяются самим приложением при старте.
/// </summary>
public sealed class ChatApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary> Подменяет строку подключения из appsettings на базу данных теста. </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Chat", connectionString);
    }
}