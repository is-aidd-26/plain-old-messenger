using ChatApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Регистрируем сервис чата, чтобы ASP.NET Core создал его сам (Dependency Injection).
builder.Services.AddSingleton<ChatService>();

WebApplication app = builder.Build();

// Список диалогов пользователя.
app.MapGet("/api/dialogs", (string user, ChatService chatService) =>
{
    try
    {
        return Results.Ok(chatService.GetDialogs(user));
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

// Сообщения диалога. after — номер последнего полученного сообщения, 0 = вся история.
app.MapGet("/api/messages", (string user, string peer, long after, ChatService chatService) =>
{
    try
    {
        return Results.Ok(chatService.GetMessages(user, peer, after));
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

// Отправка сообщения: { from, to, text }.
app.MapPost("/api/messages", (MessageRequest request, ChatService chatService) =>
{
    try
    {
        Message message = chatService.SendMessage(request.From, request.To, request.Text);
        return Results.Ok(message);
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

app.Run();