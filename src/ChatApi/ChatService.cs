namespace ChatApi;

public sealed class ChatService(ILogger<ChatService> logger)
{
    /// <summary>
    ///  Принимает новое сообщение.
    /// </summary>
    public void ReceiveMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Сообщение не может быть пустым.", nameof(text));
        }

        logger.LogInformation("Получено сообщение: {Text}", text);
    }
}