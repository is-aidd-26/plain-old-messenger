using System.Net.Http.Json;
using System.Text.Json;

namespace ChatApi.Specs.Client;

/// <summary> Клиент публичного API чата поверх HttpClient из TestServer. </summary>
public sealed class ChatClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary> Отправляет сообщение и возвращает сохранённое. </summary>
    public async Task<ApiSentMessage> SendMessageAsync(string from, string to, string text)
    {
        HttpResponseMessage response = await TrySendMessageAsync(from, to, text);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiSentMessage>(Json))
            ?? throw new InvalidOperationException("The API returned an empty response to a sent message.");
    }

    /// <summary> Пытается отправить сообщение и возвращает ответ без проверки кода. </summary>
    public Task<HttpResponseMessage> TrySendMessageAsync(string from, string to, string text)
    {
        return http.PostAsJsonAsync("/api/messages", new { from, to, text });
    }

    /// <summary> Возвращает список диалогов пользователя. </summary>
    public async Task<IReadOnlyList<ApiDialog>> GetDialogsAsync(string user)
    {
        return await http.GetFromJsonAsync<IReadOnlyList<ApiDialog>>($"/api/dialogs?user={Uri.EscapeDataString(user)}", Json)
            ?? [];
    }

    /// <summary> Запрашивает историю диалога без проверки кода ответа. </summary>
    public Task<HttpResponseMessage> TryGetMessagesAsync(long dialogId, string user, long after)
    {
        return http.GetAsync($"/api/dialogs/{dialogId}/messages?user={Uri.EscapeDataString(user)}&after={after}");
    }

    /// <summary> Возвращает сообщения диалога с номерами больше after. </summary>
    public async Task<IReadOnlyList<ApiMessage>> GetMessagesAsync(long dialogId, string user, long after)
    {
        return await ReadMessagesAsync(await TryGetMessagesAsync(dialogId, user, after));
    }

    /// <summary> Читает список сообщений из успешного ответа API. </summary>
    public static async Task<IReadOnlyList<ApiMessage>> ReadMessagesAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<ApiMessage>>(Json) ?? [];
    }

    /// <summary> Читает текст ошибки из тела ответа вида {"error": "…"}. </summary>
    public static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<ApiError>(Json) is { Error: { } error } ? error : string.Empty;
    }

    private sealed record ApiError(string Error);
}