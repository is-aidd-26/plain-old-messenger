using ChatApi;
using Npgsql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Строка подключения берётся из appsettings.json (ConnectionStrings:Chat) и переопределяется
// стандартной конфигурацией .NET, например переменной окружения ConnectionStrings__Chat.
string connectionString = builder.Configuration.GetConnectionString("Chat")
    ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Chat.");

// NpgsqlDataSource создаётся один раз и раздаёт подключения всем запросам.
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<PostgresChatStore>();
builder.Services.AddSingleton<ChatService>();

WebApplication app = builder.Build();

// Ждём готовности базы и применяем миграции до начала обслуживания запросов.
await Migrator.MigrateAsync(app.Services.GetRequiredService<NpgsqlDataSource>(), app.Logger);

// Список диалогов пользователя.
app.MapGet("/api/dialogs", async (string user, ChatService chatService) =>
{
    try
    {
        return Results.Ok(await chatService.GetDialogsAsync(user));
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

// Сообщения диалога. after — номер последнего полученного сообщения, 0 = вся история.
app.MapGet("/api/dialogs/{dialogId}/messages", async (long dialogId, string user, long after, ChatService chatService) =>
{
    try
    {
        return Results.Ok(await chatService.GetMessagesAsync(dialogId, user, after));
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
    catch (DialogNotFoundException e)
    {
        return Results.NotFound(new { error = e.Message });
    }
});

// Отправка сообщения: { from, to, text }. Диалог находится или создаётся по паре участников.
app.MapPost("/api/messages", async (MessageRequest request, ChatService chatService) =>
{
    try
    {
        return Results.Ok(await chatService.SendMessageAsync(request.From, request.To, request.Text));
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

app.Run();