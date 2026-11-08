namespace ChatApi;

/// <summary>
///  Фасад над хранилищем: проверяет входные значения, держит бизнес-правила
///  (лимиты, диалог с самим собой) и логирует отправку. SQL живёт в PostgresChatStore.
/// </summary>
public sealed class ChatService(PostgresChatStore store, ILogger<ChatService> logger)
{
    private const int MaxNicknameLength = 64;
    private const int MaxTextLength = 500;

    /// <summary>
    ///  Возвращает список диалогов пользователя.
    /// </summary>
    public Task<IReadOnlyList<DialogSummary>> GetDialogsAsync(string user)
    {
        return store.GetDialogsAsync(ValidateNickname(user, "user"));
    }

    /// <summary>
    ///  Возвращает сообщения диалога с номерами больше afterId. afterId = 0 означает всю историю.
    /// </summary>
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(long dialogId, string user, long afterId)
    {
        if (dialogId < 1)
        {
            throw new ArgumentException("Invalid dialog identifier.");
        }

        if (afterId < 0)
        {
            throw new ArgumentException("The after parameter must be a non-negative number.");
        }

        IReadOnlyList<Message>? messages = await store.GetMessagesAsync(dialogId, ValidateNickname(user, "user"), afterId);
        return messages ?? throw new DialogNotFoundException($"Dialog {dialogId} was not found.");
    }

    /// <summary>
    ///  Отправляет сообщение от from к to и возвращает сохранённое сообщение
    ///  вместе с идентификатором диалога. Незнакомые пользователи создаются лениво.
    /// </summary>
    public async Task<SentMessage> SendMessageAsync(string from, string to, string text)
    {
        string author = ValidateNickname(from, "sender");
        string peer = ValidateNickname(to, "recipient");
        string trimmedText = ValidateText(text);

        SentMessage message = await store.SendMessageAsync(author, peer, trimmedText, DateTimeOffset.UtcNow);
        logger.LogInformation("Message from {From} to {To}: {Text}", author, peer, message.Text);
        return message;
    }

    /// <summary>
    ///  Обрезает никнейм и проверяет длину; возвращает обрезанное значение.
    /// </summary>
    private static string ValidateNickname(string? nickname, string role)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException($"The {role} nickname is not specified.");
        }

        string trimmed = nickname.Trim();
        if (trimmed.Length > MaxNicknameLength)
        {
            throw new ArgumentException($"The {role} nickname is longer than {MaxNicknameLength} characters.");
        }

        return trimmed;
    }

    /// <summary>
    ///  Обрезает текст и проверяет длину; возвращает обрезанное значение.
    /// </summary>
    private static string ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The message text cannot be blank.");
        }

        string trimmed = text.Trim();
        if (trimmed.Length > MaxTextLength)
        {
            throw new ArgumentException($"The message text is longer than {MaxTextLength} characters.");
        }

        return trimmed;
    }
}