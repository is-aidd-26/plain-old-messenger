using ChatApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Регистрируем сервис чата, чтобы ASP.NET Core создал его сам (Dependency Injection).
builder.Services.AddSingleton<ChatService>();

WebApplication app = builder.Build();

app.MapPost("/api/messages", (MessageRequest request, ChatService chatService) =>
{
    try
    {
        chatService.ReceiveMessage(request.Text);
        return Results.NoContent();
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

app.Run();